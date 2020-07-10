using System.Net;

namespace ResourceManager.CommitDataRenders
{
    /// <summary>
    /// Formats the commit information heading labels with spaces.
    /// </summary>
    public sealed class MonospacedHeaderLabelFormatter : IHeaderLabelFormatter
    {
        public string FormatLabel(string label, int desiredLength, bool appendColon = true)
        {
            return (appendColon ? (WebUtility.HtmlEncode(label) + ":") : label).PadRight(desiredLength);
        }
    }
}