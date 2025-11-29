import { Application, Graphics } from 'pixi.js';

// Simple PixiJS wrapper exposing init/render/destroy as ES module exports
let app: Application | null = null;

export async function init(container: HTMLElement) {
  if (app) return;
  app = new Application();
  // Resize to container ensures canvas fits and resizes
  await app.init({ resizeTo: container, background: 0x000000 });
  container.appendChild(app.canvas);
}

export function render(args: { players: Array<{ id: string; x: number; y: number }> }) {
  if (!app) return;
  const { players } = args;

  // Clear stage
  app.stage.removeChildren();

  for (const p of players) {
    const g = new Graphics();
    g.circle(p.x * 10, p.y * 10, 6).fill(0xff5555);
    app.stage.addChild(g);
  }
}

export function destroy() {
  if (!app) return;
  app.destroy(true);
  app = null;
}
