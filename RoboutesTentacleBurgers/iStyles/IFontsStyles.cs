namespace RoboutesTentacleBurgers.iStyles
{
    public interface IFontsStyles
    {

        public static MarkupString GetCss()
        {
            string css = @"




            /* Paste contents of iFonts.css here */






@font-face {
    font-family: 'Diablo';
    src: url('/iFonts/Diablo.ttf');
    font-weight: normal;
    font-style: normal;
}

@font-face {
    font-family: 'Digital';
    src: url('/iFonts/Digital7-rg1mL.ttf');
    font-weight: normal;
    font-style: normal;
}




.DiabloRedGlow {
    font-family: 'Diablo', sans-serif;
    font-size: 1.2rem;
    color: #FF3131; /* optional styling */
    letter-spacing: 0.5px; /* optional tweak */
    text-shadow: 0 0 10px red, 0 0 20px red, 0 0 30px red, 0 0 40px red, 0 0 50px red;
}

.DiabloWhiteGlow {
    font-family: 'Diablo', sans-serif;
    font-size: 1.2rem;
    color: ghostwhite; /* optional styling */
    letter-spacing: 0.5px; /* optional tweak */
    text-shadow:0 0 10px grey, 0 0 20px grey, 0 0 30px grey, 0 0 40px grey, 0 0 50px grey;
}

.DiabloYellow {
    font-family: 'Diablo', sans-serif;
    font-size: 1.2rem;
    color: #FFF01F; /* optional styling */
    letter-spacing: 0.5px; /* optional tweak */
    text-shadow:0 0 10px darkgoldenrod, 0 0 20px darkgoldenrod, 0 0 30px darkgoldenrod, 0 0 40px darkgoldenrod, 0 0 50px darkgoldenrod;
}
.DiabloGreen {
    font-family: 'Diablo', sans-serif;
    font-size: 1.2rem;
    color: #39ff14; /* optional styling */
    letter-spacing: 0.5px; /* optional tweak */
    text-shadow:0 0 10px green, 0 0 20px green, 0 0 30px green, 0 0 40px green, 0 0 50px green;
}
.DiabloBlueGlow {
    font-family: 'Diablo', sans-serif;
    font-size: 1.2rem;
    color: #83EEFF; /* optional styling */
    letter-spacing: 0.5px; /* optional tweak */
    text-shadow:0 0 10px blue, 0 0 20px blue, 0 0 30px blue, 0 0 40px blue, 0 0 50px blue;
}
.DigitalBlueGlow {
    font-family: 'Digital', sans-serif;
    font-size: 1.2rem;
    color: #83EEFF; /* optional styling */
    letter-spacing: 0.5px; /* optional tweak */
    text-shadow:0 0 10px blue, 0 0 20px blue, 0 0 30px blue, 0 0 40px blue, 0 0 50px blue;
}

.DigitalBlue {
    font-family: 'Digital', sans-serif;
    font-size: 1.2rem;
    color: ghostwhite; /* optional styling */
    letter-spacing: 0.5px; /* optional tweak */
  //  text-shadow:0 0 10px blue, 0 0 20px blue, 0 0 30px blue, 0 0 40px blue, 0 0 50px blue;
}






        ";
            return new MarkupString($"<style>{css}</style>");
        }


    }
}
