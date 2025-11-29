import {Application, Assets, Graphics, Sprite} from 'pixi.js';

// Simple PixiJS wrapper exposing init/render/destroy as ES module exports
let app: Application | null = null;
let bunnyTexture: any = null;

export async function init(container: HTMLElement) {
  if (app) return;
  app = new Application();
  // Resize to container ensures canvas fits and resizes
  await app.init({ resizeTo: container, background: 0x000000 });
  container.appendChild(app.canvas);

  bunnyTexture = await Assets.load('https://pixijs.com/assets/bunny.png');
}

export async function render(args: { players: Array<{ id: string; x: number; y: number }> }) {
  if (!app) return;
  const { players } = args;

  // Clear stage
  app.stage.removeChildren();

  for (const p of players) {
    // Create a new Sprite.
    const bunny = new Sprite(bunnyTexture);
    bunny.x = p.x;
    bunny.y = p.y;

    app.stage.addChild(bunny);
  }
}

export function destroy() {
  if (!app) return;
  app.destroy(true);
  app = null;
}
