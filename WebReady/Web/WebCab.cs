namespace WebReady.Web
{
    /// <summary>
    /// A resource cabinat.
    /// </summary>
    public abstract class WebCab : WebAct
    {
        // acts under this cabinet
        Map<string, WebAct> acts;

        // sub cabinets
        Map<string, WebCab> cabs;

        public void AddSub(WebCab sub)
        {
        }
    }
}