
namespace RoboutesTentacleBurgers.iStyles
{
    public interface IAppStyles
    {
        public static MarkupString GetCss()
        {
            string css = @"



::-webkit-scrollbar {
    width: 8px;
}

::-webkit-scrollbar-track {
    background: rgba(0,0,0,0.5);
}

::-webkit-scrollbar-thumb {
    background: var(--primary-color);
    border-radius: 4px;
}

    ::-webkit-scrollbar-thumb:hover {
        background: var(--text-color);
    }


.inv-row {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
    padding: 20px;
    border-left: 4px;
    background-image: linear-gradient(180deg, rgba(0, 0, 0, 0.5) 40%, blue 70%);
    box-shadow: inset 0 0 10px blue;
    z-index: 10;
    color: #83EEFF; /* optional styling */
    text-shadow: 0 0 10px blue, 0 0 20px blue, 0 0 30px blue, 0 0 40px blue, 0 0 50px blue;
}

    /* Menu links and buttons */
    .inv-row a,
    .inv-row .btn-link {
        font-family: 'Diablo', sans-serif;
        font-size: 1rem;
        color: #83EEFF; /* ✅ blue by default */
        text-decoration: none;
        margin-left: 1.5rem;
        white-space: nowrap;
        text-shadow: 0 0 10px blue, 0 0 20px blue, 0 0 30px blue, 0 0 40px blue, 0 0 50px blue;
        transition: transform 0.2s ease, color 0.2s ease;
        overflow: hidden;
        text-overflow: ellipsis;
    }

        .inv-row a:hover,
        .inv-row .btn-link:hover {
            transform: scale(1.05);
            color: #ffcc00;
            letter-spacing: 0.5px;
            text-shadow: 0 0 10px blue, 0 0 20px blue, 0 0 30px blue, 0 0 40px blue, 0 0 50px blue;
        }

/* Smooth theme transitions */
* {
    transition: background-color 0.25s ease, background-image 0.25s ease, color 0.25s ease, box-shadow 0.25s ease, text-shadow 0.25s ease;
}

:root {
    --primary-color: blue;
    --text-glow: blue;
    --text-color: #83EEFF;
    --bg-top: rgba(0,0,0,1);
    --bg-bottom: blue;
}

.page {
    position: relative;
    display: flex;
    flex-direction: column;
    background-image: linear-gradient(180deg, rgba(0,0,0,1) 40%, var(--bg-bottom) 70%);
}

main {
    flex: 1;
}

.top-row {
    display: flex;
    flex-direction: row;
    align-items: center;
    justify-content: flex-start;
    height: 3.5rem;
    padding: 0 1.5rem 0 0;
    background-image: linear-gradient(180deg, rgba(0, 0, 0, 0.5) 40%, var(--primary-color) 70%);
    box-shadow: 0 2px 10px var(--primary-color); /* remove inset, use outer shadow instead */
    z-index: 10000;
    position: sticky;
    top: 0;
    color: var(--text-color);
    text-shadow: 0 0 10px var(--text-glow), 0 0 20px var(--text-glow), 0 0 30px var(--text-glow), 0 0 40px var(--text-glow), 0 0 50px var(--text-glow);
}

    .top-row a,
    .top-row .btn-link {
        font-family: 'Diablo', sans-serif;
        font-size: 1rem;
        color: var(--text-color);
        text-decoration: none;
        margin-left: 1.5rem;
        white-space: nowrap;
        text-shadow: 0 0 10px var(--text-glow), 0 0 20px var(--text-glow), 0 0 30px var(--text-glow), 0 0 40px var(--text-glow), 0 0 50px var(--text-glow);
        transition: transform 0.2s ease, color 0.2s ease;

        /*This cause a box shadow bleed thru on text*/
        /*overflow: hidden; */
        
        text-overflow: ellipsis;
    }

        .top-row a:hover,
        .top-row .btn-link:hover {
            color: #ffcc00;
            transform: scale(1.05);
        }
        .top-row a.site-title {
            font-size: 125%;
            color: #83EEFF;
              text-shadow:0 0 10px blue, 0 0 20px blue, 0 0 30px blue, 0 0 40px blue, 0 0 50px blue;
            box-shadow: none;
        }
    .top-row .hamburger-btn {
        display: flex;
        flex-direction: column;
        justify-content: center;
        gap: 5px;
        padding: 0 0.75rem;
        margin-left: 0;
        cursor: pointer;
        text-decoration: none;
    }

        .top-row .hamburger-btn .bar {
            display: block;
            width: 28px;
            height: 3px;
            background-color: var(--text-color);
            border-radius: 4px;
            box-shadow: 0 0 6px blue, 0 0 10px blue;
            transition: transform 0.2s ease, background-color 0.2s ease;
        }

        .top-row .hamburger-btn:hover .bar {
            background-color: #ffcc00;
        }

.top-left {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 0.5rem;
}

.top-right {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 1rem;
    margin-left: auto;
    padding-right: 1rem;
}

.top-row {
    justify-content: space-between; /* update existing rule */
}
.top-row .hamburger-btn {
    background: transparent;
    border: none;
    box-shadow: none;
    padding: 0 0.75rem;
    margin-left: 0;
    cursor: pointer;
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 5px;
}
.dots-menu {
  position:relative;
}

.dots-btn {
    background: transparent;
    border: none;
    color: var(--text-color);
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0 0.5rem;
    text-shadow: 0 0 6px var(--text-glow);
    line-height: 1;
}

    .dots-btn:hover {
        color: #ffcc00;
    }

.dots-dropdown {
    position: absolute;
    top: 2.5rem;
    right: 0;
    background: #111;
    border: 1px solid var(--primary-color);
    box-shadow: 0 0 10px var(--primary-color);
    border-radius: 6px;
    padding: 0.75rem;
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    z-index: 100;
    min-width: 160px;
}

.dots-item {
    background: transparent;
    border: none;
    color: var(--text-color);
    font-family: 'Diablo', sans-serif;
    font-size: 0.9rem;
    cursor: not-allowed;
    text-align: left;
    padding: 0.25rem 0;
    opacity: 0.4;
}

.title-stack {
    display: flex;
    flex-direction: column;
    justify-content: center;
}

.mobile-clock {
    display: none;
    font-size: 0.7rem;
    color: var(--text-color);
    margin-left: 0.1rem;
}

.mobile-login-circle {
    display: none;
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: #444;
    border: 2px solid var(--primary-color);
    color: var(--text-color);
    text-decoration: none;
    align-items: center;
    justify-content: center;
    font-size: 1rem;
    box-shadow: 0 0 6px var(--primary-color);
    flex-shrink: 0;
}

/* ABOVE media query - only one instance of each */
.desktop-only {
    display: flex;
    align-items: center;
}

.mobile-only {
    display: none;
}

/* MOBILE */
@media (max-width: 768px) {
    .desktop-only {
        display: none;
    }
    .mobile-only {
        display: block;
    }
    /* Show mobile elements */
    .mobile-clock {
        font-family: 'Digital', 'Courier New', monospace;
        font-size: 0.75rem;
        color: var(--text-color);
        padding-left: 10rem; /* nudge right, adjust as needed */
        display:block;
    }
    .mobile-login-circle {
        display: flex;
    }

    /* Hide desktop elements */
    .desktop-only {
        display: none;
    }

    .clock-display {
        display: none;
    }

    /* Top row layout */
    .top-row {
        padding: 0 0.5rem 0 0;
        height: 3.5rem;
    }

    .top-right {
        gap: 0.25rem; /* tighten gap between dots and circle */
        padding-right: 0.5rem;
    }

        .top-right .dots-menu {
            order: 2; /* make sure dots is after clock */
        }

        .top-right .mobile-login-circle {
            order: 3; /* circle stays last */
        }

    /* 5.B - Remove ALL shadows on mobile for performance test */
    * {
        text-shadow: none !important;
        box-shadow: none !important;
    }

    /* Keep bar background color so hamburger still visible */
    .top-row .hamburger-btn .bar {
        background-color: var(--text-color);
        box-shadow: none !important;
    }
   
    .lavalamp-wrapper {
        display: none;
    }

    /* Hide LavaLamp on mobile */
    .lava-lamp-container {
        display: none;
    }
}

.drawer-backdrop {
    position: fixed;
    inset: 0;
    z-index: 999;
    background: rgba(0,0,0,0.5);
}

.slide-drawer {
    position: fixed;
   top: 3.5rem;

    left: 0;
    width: 280px;
    height: calc(100vh - 3.5rem);
    z-index: 1000;
    overflow-y: auto;
    transform: translateX(-100%);
    transition: transform 0.3s ease;
    background-image: linear-gradient(180deg, rgba(0,0,0,0.95) 40%, var(--bg-bottom) 70%);
    box-shadow: 4px 0 20px var(--primary-color);
    padding-top: 1rem;
}

.slide-drawer.open {
    transform: translateX(0);
}


            ";
            return new MarkupString($"<style>{css}</style>");
        }
    }
}