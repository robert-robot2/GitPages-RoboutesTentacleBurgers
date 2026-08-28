namespace RoboutesTentacleBurgers.iStyles
{
    public interface IClockStyles
    {

        public static MarkupString GetCss()
        {
            string css = @"




            /* Paste contents of iFonts.css here */





.clock-display {
    font-family: 'Digital', sans-serif;
    font-size: 1.2rem;
    color: #00ffcc;
    text-align: center;
    padding-left:2rem;
}



















        ";
            return new MarkupString($"<style>{css}</style>");
        }


    }
}
