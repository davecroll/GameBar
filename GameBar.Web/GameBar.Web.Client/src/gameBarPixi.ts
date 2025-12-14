import {Application, Assets, Sprite, Texture, Rectangle} from 'pixi.js';

// Simple PixiJS wrapper exposing init/render/destroy as ES module exports
let app: Application | null = null;
let textures: Record<string, Texture> = {} as any;

// New: .NET callback reference and ticker wiring
let dotNetRef: any | null = null;
let loopStarted = false;

// Cache for per-player sprites so we don't recreate/destroy every frame
const playerSprites: Map<string, Sprite> = new Map();

export function setDotNetRef(ref: any) {
  dotNetRef = ref;
}

export async function init(container: HTMLElement) {
  if (app) return;
  app = new Application();
  // Resize to container ensures canvas fits and resizes
  await app.init({ resizeTo: container, background: 0x000000 });
  container.appendChild(app.canvas);
}

export async function loadAsset(key: string, url: string) {
  if (!app) return;
  textures[key] = await Assets.load(url) as Texture;
}

export async function render(args: { players: Array<{ id: string; x: number; y: number; frameIndex: number; anim: string; frameWidth: number; frameHeight: number }> }) {
  if (!app) return;
  const { players } = args;

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
}

export function startLoop() {
  if (!app || !dotNetRef || loopStarted) return;
  loopStarted = true;
  app.ticker.add(async () => {
    if (!dotNetRef) return;
    try {
      const players = await dotNetRef.invokeMethodAsync('GetRenderPlayersAsync');
      await render({ players });
    } catch (e) {
      // swallow per-frame errors to avoid breaking the ticker
      // console.error('Pixi loop error', e);
    }
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
  app.destroy(true);
  app = null;
}
