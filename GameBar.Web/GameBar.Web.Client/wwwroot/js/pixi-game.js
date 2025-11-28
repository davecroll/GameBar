window.gameBarPixi = (function () {
    let app;
    const playerSprites = new Map();

    async function init(container) {
        console.info("pixi-game.js: init start");

        app = new PIXI.Application();
        
        await app.init({
            width: 800,
            height: 600,
            backgroundColor: 0x202020,
        });

        container.appendChild(app.canvas ?? app.view ?? app.renderer?.view ?? app.renderer?.canvas);

        console.info("pixi-game.js: init end");
    }

    function ensurePlayerSprite(id) {
        let sprite = playerSprites.get(id);
        if (!sprite) {
            sprite = new PIXI.Graphics();
            sprite.circle(0, 0, 10).fill(0x00ff00);
            app.stage.addChild(sprite);
            playerSprites.set(id, sprite);
        }
        return sprite;
    }

    function render(state) {
        if (!app) return;

        const seen = new Set();

        if (state.players) {
            state.players.forEach(p => {
                const sprite = ensurePlayerSprite(p.id);
                sprite.x = p.x * 10 + 400; // scale & center
                sprite.y = p.y * 10 + 300;
                seen.add(p.id);
            });
        }

        for (const [id, sprite] of playerSprites.entries()) {
            if (!seen.has(id)) {
                app.stage.removeChild(sprite);
                sprite.destroy();
                playerSprites.delete(id);
            }
        }
    }

    return {
        init,
        render,
    };
})();
