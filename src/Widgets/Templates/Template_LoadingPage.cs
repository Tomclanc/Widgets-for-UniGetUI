
namespace WidgetsForUniGetUI.Templates
{
    internal class Template_LoadingPage : AbstractTemplate
    {
        public Template_LoadingPage(GenericWidget widget) : base(widget)
        { }

        protected override string GenerateTemplateBody()
        {
            return TemplateBody;
        }

        protected override string GenerateTemplateData()
        {
            return "{}";
        }

        private const string TemplateBody = $$"""
            {
                "type": "Container",
                "items": [
                    {
                        "type": "Container",
                        "items": [
                            {
                                "type": "TextBlock",
                                "text": "正在检查更新...",
                                "wrap": true,
                                "fontType": "Default",
                                "size": "Default",
                                "weight": "Bolder",
                                "horizontalAlignment": "Center"
                            },
                            {
                                "type": "Image",
                                "url": "https://marticliment.com/resources/widgets/sandclock.png",
                                "horizontalAlignment": "Center",
                                "size": "Medium",
                                "$when": "${$host.widgetSize!=\"small\"}"
                            },
                            {
                                "type": "TextBlock",
                                "text": "这不会花太久",
                                "wrap": true,
                                "size": "Small",
                                "horizontalAlignment": "Center"
                            }
                        ],
                        "verticalContentAlignment": "Center",
                        "height": "stretch"
                    }
                ],
                "id": "LoadingDiv",
                "height": "stretch",
                "verticalContentAlignment": "Center"
            }
            """;
    }
}
