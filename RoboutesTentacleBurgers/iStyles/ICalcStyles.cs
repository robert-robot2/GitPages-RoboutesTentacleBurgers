using Microsoft.AspNetCore.Components;

namespace RoboutesTentacleBurgers.iStyles
{
    public interface ICalcStyles
    {
        public static MarkupString GetCss()
        {
            string css = @"
/* ==================== CSS VARIABLES FOR THEMING ==================== */
:root {
    --calc-primary-color: #ff0000;
    --calc-secondary-color: #ff4444;
    --calc-glow-color: #ff0000;
    --calc-display-text: #00ff00;
    --calc-button-text: #ff4444;
    --calc-border-color: #ff0000;
}

/* ==================== CALCULATOR SHELL ==================== */
.calculon-shell {
    position: relative;
    width: 420px;
    height: 610px;
    background-image: url('/iAssets/Calculon005.png');
    background-size: cover;
    background-position: center;
    border: 2px solid var(--calc-border-color);
    box-shadow: 0 0 20px var(--calc-glow-color);
    border-radius: 12px;
    overflow: hidden;
    margin: 0 auto;
}

/* ==================== DISPLAY ==================== */
.calculon-display {
    position: absolute;
    top: 20px;
    left: 10px;
    width: 370px;
    height: 50px;
    background-color: rgba(0, 0, 0, 0.9);
    border: 2px solid var(--calc-border-color);
    border-radius: 6px;
    box-shadow: 0 0 10px var(--calc-glow-color), 0 0 20px var(--calc-secondary-color);
    display: flex;
    align-items: center;
    justify-content: flex-end;
    padding: 0 12px;
    z-index: 15;
}

.display-value {
    color: var(--calc-display-text);
    font-family: 'Courier New', monospace;
    font-size: 1.6rem;
    font-weight: bold;
    text-shadow: 0 0 8px var(--calc-display-text);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    flex: 1;
    text-align: right;
}

.angle-mode-indicator {
    position: absolute;
    top: 2px;
    left: 8px;
    color: var(--calc-secondary-color);
    font-family: 'Diablo', sans-serif;
    font-size: 0.7rem;
    text-shadow: 0 0 5px var(--calc-glow-color);
}

.memory-indicator {
    position: absolute;
    top: 2px;
    right: 8px;
    color: var(--calc-secondary-color);
    font-family: 'Diablo', sans-serif;
    font-size: 0.7rem;
    text-shadow: 0 0 5px var(--calc-glow-color);
}

/* ==================== BUTTON GRID (6 × 9) ==================== */
.calculon-grid {
    position: absolute;
    top: 90px;
    left: 15px;
    width: 390px;
    height: 450px;
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    grid-template-rows: repeat(9, 1fr);
    gap: 4px;
    z-index: 10;
}

.calc-btn {
    background-color: transparent;
    color: var(--calc-button-text);
    font-family: 'Diablo', sans-serif;
    font-size: 1rem;
    border: none;
    padding: 0;
    cursor: pointer;
    text-shadow: 0 0 5px var(--calc-glow-color);
    transition: all 0.2s ease;
    display: flex;
    align-items: center;
    justify-content: center;
    user-select: none;
}

.calc-btn:hover {
    color: #ffcc00;
    text-shadow: 0 0 10px #ffcc00;
    transform: scale(1.1);
}

.calc-btn:active,
.calc-btn.button-pressed {
    transform: scale(0.95);
    text-shadow: 0 0 15px var(--calc-glow-color);
}

.number-btn {
    font-family: 'Digital', 'Courier New', monospace;
    font-weight: 600;
    font-size: 1.2rem;
}

/* ==================== WIDE BUTTONS ROW ==================== */
.wide-buttons-row {
    position: absolute;
    bottom: 10px;
    left: 15px;
    width: 390px;
    height: 45px;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 8px;
    z-index: 10;
}

.wide-btn {
    background-color: rgba(0, 0, 0, 0.7);
    color: var(--calc-button-text);
    font-family: 'Diablo', sans-serif;
    font-size: 1rem;
    border: 2px solid var(--calc-border-color);
    border-radius: 6px;
    cursor: pointer;
    text-shadow: 0 0 5px var(--calc-glow-color);
    box-shadow: 0 0 8px var(--calc-glow-color);
    transition: all 0.2s ease;
    display: flex;
    align-items: center;
    justify-content: center;
    user-select: none;
}

.wide-btn:hover {
    background-color: rgba(var(--calc-primary-color), 0.3);
    color: #ffcc00;
    text-shadow: 0 0 10px #ffcc00;
    box-shadow: 0 0 15px var(--calc-glow-color);
    transform: scale(1.02);
}

.wide-btn:active {
    transform: scale(0.98);
    box-shadow: 0 0 20px var(--calc-glow-color);
}

.theme-btn span,
.bg-btn span {
    font-size: 0.9rem;
    font-weight: bold;
}

/* ==================== MOBILE RESPONSIVE ==================== */
@media (max-width: 480px) {
    .calculon-shell {
        width: 100%;
        max-width: 420px;
        height: auto;
        aspect-ratio: 420 / 610;
    }

    .calculon-display {
        width: calc(100% - 30px);
    }

    .calculon-grid {
        width: calc(100% - 30px);
        height: calc(73% - 60px);
    }

    .wide-buttons-row {
        width: calc(100% - 30px);
    }

    .calc-btn {
        font-size: 0.9rem;
    }

    .number-btn {
        font-size: 1.1rem;
    }
}


/* ==================== ANIMATIONS ==================== */
@keyframes button-pulse {
    0%, 100% {
        text-shadow: 0 0 5px var(--calc-glow-color);
    }
    50% {
        text-shadow: 0 0 15px var(--calc-glow-color), 0 0 25px var(--calc-secondary-color);
    }
}

.calc-btn:active {
    animation: button-pulse 0.3s ease;
}


            ";
            return new MarkupString($"<style>{css}</style>");
        }
    }
}