

namespace WidgetsForUniGetUI.Templates
{
    internal class Template_MissingUniGetUI : AbstractTemplate
    {
        public Template_MissingUniGetUI(GenericWidget widget) : base(widget)
        { }

        protected override string GenerateTemplateBody()
        {
            return TemplateBody;
        }

        protected override string GenerateTemplateData()
        {
            return "{}";
        }

        public const string TemplateBody = $$"""
            {
                "type": "Container",
                "items": [
                    {
                        "type": "Container",
                        "items": [
                            {
                                "type": "TextBlock",
                                "text": "无法与 UniGetUI 通信",
                                "wrap": true,
                                "horizontalAlignment": "Center",
                                "fontType": "Default",
                                "size": "Default",
                                "weight": "Bolder"
                            },
                            {
                                "type": "Image",
                                "url": "https://marticliment.com/resources/widgets/unigetui_new.png",
                                "horizontalAlignment": "Center",
                                "size": "Medium",
                                "$when": "${$host.widgetSize!=\"small\"}"
                            },
                            {
                                "type": "TextBlock",
                                "text": "此小组件需要 UniGetUI 才能工作。\n请确认 UniGetUI 已安装并正在后台运行",
                                "wrap": true,
                                "horizontalAlignment": "Center",
                                "size": "Small"
                            }
                        ],
                        "verticalContentAlignment": "Center",
                        "height": "stretch"
                    },
                    {
                        "type": "ColumnSet",
                        "columns": [
                            {
                                "type": "Column",
                                "width": "stretch",
                                "spacing": "None",
                                "height": "stretch",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "text": " ",
                                        "wrap": true
                                    }
                                ]
                            },
                            {
                                "type": "Column",
                                "width": "auto",
                                "items": [
                                    {
                                        "type": "ActionSet",
                                        "actions": [
                                            {
                                                "type": "Action.Execute",
                                                "title": "重试",
                                                "verb": "{{Verbs.Reload}}"
                                            }
                                        ]
                                    }
                                ],
                                "spacing": "None",
                                "height": "stretch"
                            },
                            {
                                "type": "Column",
                                "width": "stretch",
                                "spacing": "None",
                                "height": "stretch",
                                "items": [
                                    {
                                        "type": "TextBlock",
                                        "text": " ",
                                        "wrap": true
                                    }
                                ]
                            }
                        ],
                        "spacing": "Small"
                    }
                ],
                "verticalContentAlignment": "Center",
                "style": "default",
                "id": "NoWingetUIDiv",
                "height": "stretch"
            }
            """;
    }
}
