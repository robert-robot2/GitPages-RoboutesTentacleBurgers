using Microsoft.AspNetCore.Components;

namespace RoboutesTentacleBurgers.iStyles
{
    public interface IChemCalcStyles
    {
        public static MarkupString GetCss()
        {
            string css = @"
/* ==================== CHEMCALC - COMPLETE CSS ==================== */
/* 1920×1080 Chemical Engineering Calculator */

/* ==================== PAGE LAYOUT ==================== */
.chemcalc-page {
    width: 1920px;
    height: 1080px;
    background-color: #000000;
    color: #ffffff;
    font-family: 'Arial', sans-serif;
    overflow: hidden;
    margin: 0 auto;
}

.chemcalc-container {
    width: 100%;
    height: 100%;
    display: grid;
    grid-template-columns: 200px 1fr 200px;
    gap: 10px;
    padding: 10px;
}

/* ==================== LEFT PANEL - OUTPUT ==================== */
.chemcalc-left {
    background-color: #1a1a1a;
    border: 2px solid #ff4444;
    border-radius: 8px;
    overflow-y: auto;
}

.output-panel {
    padding: 15px;
}

.output-header {
    font-size: 1.2rem;
    font-weight: bold;
    color: #ff4444;
    margin-bottom: 15px;
    text-align: center;
    border-bottom: 2px solid #ff4444;
    padding-bottom: 5px;
}

.element-detail-card {
    background-color: #2a2a2a;
    border: 1px solid #ff4444;
    border-radius: 6px;
    padding: 15px;
    margin-bottom: 15px;
}

.element-detail-symbol {
    font-size: 3rem;
    font-weight: bold;
    text-align: center;
    color: #ff6666;
    margin-bottom: 10px;
}

.element-detail-name {
    font-size: 1.2rem;
    text-align: center;
    color: #ffaaaa;
    margin-bottom: 15px;
}

.element-detail-info {
    font-size: 0.9rem;
}

.detail-row {
    display: flex;
    justify-content: space-between;
    margin-bottom: 8px;
    padding: 5px;
    background-color: #1a1a1a;
    border-radius: 4px;
}

.detail-label {
    color: #aaaaaa;
}

.detail-value {
    color: #ffffff;
    font-weight: bold;
}

.output-placeholder {
    text-align: center;
    color: #666666;
    padding: 40px 20px;
    font-style: italic;
}

/* ==================== REACTION TYPE DISPLAY ==================== */
.reaction-type-display {
    background-color: #2a2a2a;
    border: 2px solid #ffaa00;
    border-radius: 8px;
    padding: 12px;
    margin: 10px 15px;
    box-shadow: 0 0 10px rgba(255, 170, 0, 0.3);
    transition: all 0.3s ease;
}

.reaction-type-label {
    font-size: 0.85rem;
    color: #ffaa00;
    margin-bottom: 5px;
    text-transform: uppercase;
    letter-spacing: 1px;
}

.reaction-type-value {
    font-size: 1.1rem;
    color: #ffffff;
    font-weight: bold;
    text-shadow: 0 0 5px rgba(255, 170, 0, 0.5);
}

/* ==================== CHEM BUTTON TAB ==================== */
.chem-button-tab {
    background-color: #2a2a2a;
    border: 2px solid #ff00ff;
    border-radius: 8px;
    padding: 15px;
    margin: 10px 15px;
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.chem-btn {
    background-color: #1a1a1a;
    color: #ff00ff;
    border: 2px solid #ff00ff;
    border-radius: 6px;
    padding: 12px;
    cursor: pointer;
    font-size: 0.9rem;
    font-weight: bold;
    transition: all 0.2s ease;
    text-align: center;
}

.chem-btn:hover {
    background-color: rgba(255, 0, 255, 0.2);
    transform: scale(1.05);
    box-shadow: 0 0 10px rgba(255, 0, 255, 0.5);
}

.chem-btn:active {
    transform: scale(0.95);
}

.mode-toggle-btn {
    background-color: #2a2a2a;
    border-color: #00ffff;
    color: #00ffff;
}

.mode-toggle-btn:hover {
    background-color: rgba(0, 255, 255, 0.2);
    box-shadow: 0 0 10px rgba(0, 255, 255, 0.5);
}

.mode-toggle-btn.mode-active {
    background-color: rgba(0, 255, 255, 0.4);
    border-color: #ffcc00;
    color: #ffcc00;
    box-shadow: 0 0 15px rgba(255, 204, 0, 0.7);
}



.molecule-display {
    background-color: #2a2a2a;
    border: 1px solid #4444ff;
    border-radius: 6px;
    padding: 15px;
    margin-top: 15px;
}

.molecule-header {
    font-size: 1rem;
    color: #6666ff;
    margin-bottom: 10px;
    text-align: center;
}

.molecule-formula {
    font-size: 2rem;
    font-weight: bold;
    text-align: center;
    color: #ffffff;
    margin-bottom: 10px;
}

.molecule-mass {
    font-size: 0.9rem;
    text-align: center;
    color: #aaaaaa;
}

/* ==================== CENTER PANEL ==================== */
.chemcalc-center {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

/* ==================== CALCULATOR SECTION ==================== */
.chemcalc-calculator-section {
    background-color: #1a1a1a;
    border: 2px solid #ff0000;
    border-radius: 8px;
    padding: 10px;
    height: 175px;
    position: absolute;
    left: 395px;
    top: 150px;
    width: 775px;
    z-index: 9;
}
.section-label {
    font-size: 0.9rem;
    color: #ffffff;
    text-align: center;
    margin-bottom: 5px;
    background-color: #ff0000;
    padding: 5px;
    border-radius: 4px;
}

.calculator-panel {
    display: flex;
    flex-direction: column;
    width: 100%;
    height: 100%;
    position: relative;
    z-index: 10;
}

.calculator-display {
    background-color: #000000;
    border: 2px solid #ff4444;
    border-radius: 6px;
    padding: 5px 10px;
    height: 35px;
    display: flex;
    align-items: center;
    justify-content: flex-end;
    position: relative;
    margin-bottom: 5px;
}

.calc-mode-indicator {
    position: absolute;
    left: 8px;
    font-size: 0.7rem;
    color: #ff4444;
}

.calc-memory-indicator {
    position: absolute;
    right: 8px;
    top: 2px;
    font-size: 0.7rem;
    color: #ff4444;
}

.calc-display-value {
    font-size: 1.2rem;
    font-family: 'Courier New', monospace;
    color: #00ff00;
    text-shadow: 0 0 5px #00ff00;
}


/* ==================== BUTTON GRID (6 × 9) ==================== */
.calculator-grid {
    display: grid !important;
    grid-template-columns: repeat(10, 1fr); /* 10 columns across */
    grid-template-rows: repeat(4, 1fr);     /* 4 rows down */
    gap: 4px;                               /* space between buttons */
    width: 70%;
    height: 600px;                           /* adjust to fit your panel */
}

.calculator-grid > .calc-btn {
    width: 100%;
    height: 100%;
    padding: 0;
    font-size: 0.9rem;                       /* smaller, fits better */
    box-sizing: border-box;
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
z-index:10;
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
.calc-btn.chem-mode-active {
    color: #ffcc00 !important;
    text-shadow: 0 0 10px #ffcc00 !important;
    font-weight: bold;
}

.calc-btn.chem-mode-active:hover {
    color: #ffff00 !important;
    text-shadow: 0 0 15px #ffff00 !important;
    transform: scale(1.15);
}

/* ==================== EQUATION SECTION ==================== */
.chemcalc-equation-section {
    background-color: #1a1a1a;
    border: 2px solid #4444ff;
    border-radius: 8px;
    padding: 15px;
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.equation-display {
    width: 100%;
    text-align: center;
}

.equation-content {
    font-size: 1.5rem;
    color: #ffffff;
    font-weight: bold;
}

.equation-type {
    font-size: 0.9rem;
    color: #6666ff;
    margin-top: 5px;
}

.equation-placeholder {
    color: #666666;
    font-style: italic;
}

/* ==================== PERIODIC TABLE SECTION ==================== */
.chemcalc-periodic-section {
    background-color: #1a1a1a;
    border: 2px solid #44ff44;
    border-radius: 8px;
    padding: 15px;
    flex: 1;
}

.periodic-table-grid {
    display: grid;
    grid-template-columns: repeat(18, 1fr);
    grid-template-rows: repeat(9, 1fr);
    gap: 4px;
    height: 100%;
}

.element-card {
    background-color: #2a2a2a;
    border: 2px solid #444444;
    border-radius: 4px;
    padding: 4px;
    cursor: pointer;
    transition: all 0.2s ease;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    font-size: 0.7rem;
}

.element-card:hover {
    transform: scale(1.1);
    border-color: #ffcc00;
    box-shadow: 0 0 10px rgba(255, 204, 0, 0.5);
    z-index: 10;
}

.element-card.element-highlighted {
    border: 3px solid #ffcc00;
    box-shadow: 0 0 15px rgba(255, 204, 0, 0.7);
}

.element-number {
    font-size: 0.6rem;
    color: #aaaaaa;
    position: absolute;
    top: 2px;
    left: 2px;
}

.element-symbol {
    font-size: 1.2rem;
    font-weight: bold;
    color: #000000;
    margin: 2px 0;
}

.element-name {
    font-size: 0.6rem;
    color: #000000;
    text-align: center;
}

.element-mass {
    font-size: 0.55rem;
    color: #000000;
}

.element-clickable {
    position: relative;
}

/* ==================== CATEGORY LEGEND SECTION ==================== */
.chemcalc-legend-section {
    background-color: #1a1a1a;
    border: 2px solid #ff44ff;
    border-radius: 8px;
    padding: 10px;
    height: 60px;
}

.category-legend {
    display: flex;
    gap: 8px;
    justify-content: center;
    align-items: center;
    flex-wrap: wrap;
}

.category-btn {
    padding: 8px 12px;
    border: 2px solid #ffffff;
    border-radius: 6px;
    cursor: pointer;
    font-size: 0.8rem;
    font-weight: bold;
    color: #000000;
    transition: all 0.2s ease;
}

.category-btn:hover {
    transform: scale(1.1);
    box-shadow: 0 0 10px rgba(255, 255, 255, 0.5);
}

.category-btn.category-active {
    border: 3px solid #ffcc00;
    box-shadow: 0 0 15px rgba(255, 204, 0, 0.8);
}

/* ==================== RIGHT PANEL - STATE SELECTOR ==================== */
.chemcalc-right {
    background-color: #1a1a1a;
    border: 2px solid #44ffff;
    border-radius: 8px;
    overflow-y: auto;
}

.state-selector {
    padding: 15px;
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.state-header {
    font-size: 1.2rem;
    font-weight: bold;
    color: #44ffff;
    text-align: center;
    border-bottom: 2px solid #44ffff;
    padding-bottom: 5px;
    margin-bottom: 10px;
}

.state-btn {
    background-color: rgba(68, 255, 255, 0.2);
    color: #44ffff;
    border: 2px solid #44ffff;
    border-radius: 6px;
    padding: 12px;
    cursor: pointer;
    font-size: 0.9rem;
    transition: all 0.2s ease;
}

.state-btn:hover {
    background-color: rgba(68, 255, 255, 0.4);
    transform: scale(1.05);
}

.state-btn.state-active {
    background-color: rgba(68, 255, 255, 0.6);
    border-color: #ffcc00;
    box-shadow: 0 0 10px rgba(255, 204, 0, 0.5);
}

.state-info {
    margin-top: 15px;
    text-align: center;
}

.current-state {
    color: #44ffff;
    font-size: 0.9rem;
    padding: 8px;
    background-color: #2a2a2a;
    border-radius: 4px;
}

.future-features {
    margin-top: 30px;
    padding: 15px;
    background-color: #2a2a2a;
    border: 1px solid #666666;
    border-radius: 6px;
}

.future-header {
    font-size: 1rem;
    color: #666666;
    text-align: center;
    margin-bottom: 10px;
    font-weight: bold;
}

.future-item {
    color: #888888;
    font-size: 0.85rem;
    padding: 8px;
    margin-bottom: 5px;
    background-color: #1a1a1a;
    border-radius: 4px;
    text-align: center;
}



.molecule-display-content {
    width: 70%;
    text-align: center;
}

.molecule-placeholder {
    width: 100%;
    text-align: center;
    color: #666666;
    font-style: italic;
    font-size: 0.9rem;
}

/* ==================== MOLECULE SECTION ==================== */
.chemcalc-molecule-section {
    background-color: #1a1a1a;
    border: 2px solid #4444ff;
    border-radius: 8px;
    padding: 8px;           /* Reduced from 10px */
    height: 80px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.molecule-header {
    font-size: 0.75rem;      /* Reduced from 0.9rem */
    color: #6666ff;
    margin-bottom: 3px;      /* Reduced from 5px */
}

.molecule-formula {
    font-size: 1.5rem;       /* Reduced from 1.8rem */
    font-weight: bold;
    color: #ffffff;
    margin-bottom: 3px;      /* Reduced from 5px */
}

.molecule-mass {
    font-size: 0.75rem;      /* Reduced from 0.85rem */
    color: #aaaaaa;
}




/* ==================== RESPONSIVE (if needed) ==================== */
@media (max-width: 1920px) {
    .chemcalc-page {
        width: 100%;
        height: auto;
        min-height: 100vh;
    }
}
            ";
            return new MarkupString($"<style>{css}</style>");
        }
    }
}