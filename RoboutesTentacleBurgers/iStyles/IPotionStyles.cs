namespace RoboutesTentacleBurgers.iStyles
{
    public interface IPotionStyles
    {

        public static MarkupString GetCss()
        {
            string css = @"




            /* Paste contents of iFonts.css here */



.heal-container {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px;
}

.heal-potion {
    width: 64px;
    height: 64px;
    cursor: pointer;
    transition: transform 0.2s ease;
}

    .heal-potion:hover {
        transform: scale(1.1);
    }

.heal-text {
    font-family: 'Diablo', sans-serif;
    font-size: 1.2rem;
    color: #ff4444;
    text-shadow: 0 0 5px #ff0000, 0 0 10px #ff4444;
    transition: opacity 0.3s ease;
}



















        ";
            return new MarkupString($"<style>{css}</style>");
        }


    }
}
