import {Application, Assets, Sprite, Texture, Rectangle} from 'pixi.js';
import { Graphics } from 'pixi.js';

// --- Interfaces ---

interface PlayerSnapshotData {
  id: string;
  x: number;
  y: number;
  movementStateName: string;
  movementStateStartTick: number;
  actionStateName: string | null;
  actionStateStartTick: number | null;
}

interface SnapshotData {
  serverTick: number;
  players: PlayerSnapshotData[];
}

interface AnimationMeta {
  assetKey: string;
  frameCount: number;
  frameWidth: number;
  frameHeight: number;
  frameDurationMs: number;
  loop: boolean;
}

interface AnimationManifest {
  tickDurationMs: number;
  states: Record<string, AnimationMeta>;
}

interface RenderPlayer {
  id: string;
  x: number;
  y: number;
  frameIndex: number;
  anim: string;
  frameWidth: number;
  frameHeight: number;
}

// --- Module state ---

let app: Application | null = null;
let textures: Record<string, Texture> = {} as any;

// Debug overlay
let debugGraphics: Graphics | null = null;
let debugLineY: number | null = 400;

let loopStarted = false;

// Cache for per-player sprites so we don't recreate/destroy every frame
const playerSprites: Map<string, Sprite> = new Map();

// Snapshot double-buffer for interpolation
let prevSnapshot: SnapshotData | null = null;
let currSnapshot: SnapshotData | null = null;
let prevReceiveTime = 0;
let currReceiveTime = 0;

// Animation manifest (set once at init)
let manifest: AnimationManifest | null = null;

// --- Debug overlay ---

function ensureDebugOverlay() {
  if (!app) return;
  if (!debugGraphics) {
    debugGraphics = new Graphics();
    // Keep it in the background: insert at index 0 so sprites render above it.
    app.stage.addChildAt(debugGraphics, 0);
  }
}

function redrawDebugOverlay() {
  if (!app) return;
  if (debugLineY == null) {
    if (debugGraphics) debugGraphics.clear();
    return;
  }
  ensureDebugOverlay();
  if (!debugGraphics) return;

  const y = debugLineY;
  // Clear and redraw. Use device pixels in world coords.
  debugGraphics.clear();
  debugGraphics
    .moveTo(0, y)
    .lineTo(app.renderer.width, y)
    .stroke({ color: 0xffffff, width: 1, alpha: 0.4 });

  // Keep it behind everything else even if other things get added later.
  app.stage.setChildIndex(debugGraphics, 0);
}

// --- Exports ---

export function pushSnapshot(data: SnapshotData) {
  prevSnapshot = currSnapshot;
  prevReceiveTime = currReceiveTime;
  currSnapshot = data;
  currReceiveTime = performance.now();
}

export function setManifest(m: AnimationManifest) {
  manifest = m;
}

export async function init(container: HTMLElement) {
  if (app) return;
  app = new Application();
  // Resize to container ensures canvas fits and resizes
  await app.init({ resizeTo: container, background: 0x000000 });
  container.appendChild(app.canvas);

  // Draw the debug line once, and again on resize.
  redrawDebugOverlay();
  app.renderer.on('resize', () => redrawDebugOverlay());
}

export async function loadAsset(key: string, url: string) {
  if (!app) return;
  textures[key] = await Assets.load(url) as Texture;
}

// Call from .NET/Blazor if you want to move or hide the line at runtime.
// Set y to null to hide.
export function setDebugLineY(y: number | null) {
  debugLineY = y;
  redrawDebugOverlay();
}

// --- Interpolation & animation ---

function interpolateAndAnimate(now: number): RenderPlayer[] {
  if (!currSnapshot || !manifest) return [];

  const tickDurationMs = manifest.tickDurationMs;

  // Build lookup for prev snapshot players
  const prevPlayers = new Map<string, PlayerSnapshotData>();
  if (prevSnapshot) {
    for (const p of prevSnapshot.players) {
      prevPlayers.set(p.id, p);
    }
  }

  // Compute interpolation factor
  let t = 0;
  if (prevSnapshot && currReceiveTime > prevReceiveTime) {
    const elapsed = now - currReceiveTime;
    const interval = currReceiveTime - prevReceiveTime;
    t = Math.max(0, Math.min(1, elapsed / interval));
  }

  const result: RenderPlayer[] = [];

  for (const player of currSnapshot.players) {
    const prev = prevPlayers.get(player.id);

    // Interpolate position
    let x: number, y: number;
    if (prev) {
      x = prev.x + (player.x - prev.x) * t;
      y = prev.y + (player.y - prev.y) * t;
    } else {
      // New player or first snapshot — no lerp
      x = player.x;
      y = player.y;
    }

    // Determine active animation state
    const stateName = player.actionStateName || player.movementStateName;
    const meta = manifest.states[stateName] ?? manifest.states['Idle'];
    if (!meta) continue;

    // Compute animation frame index from state start tick
    const startTick = player.actionStateName
      ? (player.actionStateStartTick ?? player.movementStateStartTick)
      : player.movementStateStartTick;

    const currentServerTimeMs = currSnapshot.serverTick * tickDurationMs;
    const startTimeMs = startTick * tickDurationMs;
    const elapsedMs = Math.max(0, currentServerTimeMs - startTimeMs);

    const frameDurationMs = Math.max(1, meta.frameDurationMs);
    const frames = Math.max(1, meta.frameCount);

    const steps = Math.floor(elapsedMs / frameDurationMs);
    const frameIndex = meta.loop
      ? steps % frames
      : Math.min(frames - 1, steps);

    result.push({
      id: player.id,
      x,
      y,
      frameIndex,
      anim: meta.assetKey,
      frameWidth: meta.frameWidth,
      frameHeight: meta.frameHeight,
    });
  }

  return result;
}

// --- Render ---

function renderPlayers(players: RenderPlayer[]) {
  if (!app) return;

  const seenIds = new Set<string>();

  for (const p of players) {
    seenIds.add(p.id);

    let sprite = playerSprites.get(p.id);
    const baseTex = textures[p.anim] ?? textures['idle'];
    if (!baseTex) continue;

    const idx = p.frameIndex;
    const x = idx * p.frameWidth;
    const y = 0;

    const rect = new Rectangle(x, y, p.frameWidth, p.frameHeight);
    const frame = new Texture({ source: baseTex.source, frame: rect });

    if (!sprite) {
      sprite = new Sprite(frame);
      sprite.anchor.set(0.5, 0.5);
      playerSprites.set(p.id, sprite);
      app.stage.addChild(sprite);
    } else {
      // Reuse existing sprite and just update its texture
      sprite.texture = frame;
    }

    sprite.x = p.x;
    sprite.y = p.y;
    sprite.visible = true;
  }

  // Hide/remove sprites for players that no longer exist in the snapshot
  for (const [id, sprite] of playerSprites.entries()) {
    if (!seenIds.has(id)) {
      sprite.visible = false;
    }
  }

  // Re-assert debug overlay ordering/drawing (keeps it behind sprites).
  redrawDebugOverlay();
}

// --- Loop ---

export function startLoop() {
  if (!app || loopStarted) return;
  loopStarted = true;
  app.ticker.add(() => {
    const now = performance.now();
    const players = interpolateAndAnimate(now);
    renderPlayers(players);
  });
}

export function stopLoop() {
  if (!app) return;
  loopStarted = false;
  app.ticker.stop();
}

export function destroy() {
  if (!app) return;
  playerSprites.clear();
  debugGraphics?.destroy();
  debugGraphics = null;
  app.destroy(true);
  app = null;
  prevSnapshot = null;
  currSnapshot = null;
  manifest = null;
}
