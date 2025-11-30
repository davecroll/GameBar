import {Application, Assets, Sprite, Texture, Rectangle} from 'pixi.js';

// Simple PixiJS wrapper exposing init/render/destroy as ES module exports
let app: Application | null = null;
let textures: Record<string, Texture> = {} as any;

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

export async function render(args: { players: Array<{ id: string; x: number; y: number; frameIndex: number; anim: string }> }) {
  if (!app) return;
  const { players } = args;

  // Clear stage
  app.stage.removeChildren();

  for (const p of players) {
    const baseTex = textures[p.anim] ?? textures['idle'];
    if (!baseTex) continue;

    const frameWidth = 48;
    const frameHeight = 48;
    // framesCount is not needed here because we clamp modulo on client
    const idx = p.frameIndex;

    // Compute source rectangle on the spritesheet (horizontal strip)
    const x = idx * frameWidth;
    const y = 0;

    const rect = new Rectangle(x, y, frameWidth, frameHeight);
    const frame = new Texture({ source: baseTex.source, frame: rect });

    const sprite = new Sprite(frame);
    sprite.x = p.x;
    sprite.y = p.y;
    sprite.anchor.set(0.5, 0.5);

    app.stage.addChild(sprite);
  }
}

export function destroy() {
  if (!app) return;
  app.destroy(true);
  app = null;
}
