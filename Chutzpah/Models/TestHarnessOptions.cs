namespace Chutzpah.Models
{
    public enum TemplateMode
    {
        /// <summary>
        /// Raw html injected into the dom
        /// </summary>
        Raw,

        /// <summary>
        /// Html wrapped in a script tag
        /// </summary>
        Script
    }

    public class TemplateOptions
    {
        /// <summary>
        /// The mode that the template is being injected
        /// </summary>
        public TemplateMode Mode { get; set; } 

        /// <summary>
        /// If in script mode what Id to place on the script tag
        /// </summary>
        public string Id { get; set; }


        /// <summary>
        /// If in script mode what Type to place on script tag
        /// </summary>
        public string Type { get; set; }

    }
}