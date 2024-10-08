using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using UiPath.LGAIResearch.Activities.Design.Designers;
using UiPath.LGAIResearch.Activities.Design.Properties;

namespace UiPath.LGAIResearch.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(AnalyzeDocument), categoryAttribute);
            builder.AddCustomAttributes(typeof(AnalyzeDocument), new DesignerAttribute(typeof(AnalyzeDocumentDesigner)));
            builder.AddCustomAttributes(typeof(AnalyzeDocument), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GenerateSVGFromSmiles), categoryAttribute);
            builder.AddCustomAttributes(typeof(GenerateSVGFromSmiles), new DesignerAttribute(typeof(GenerateSVGFromSmilesDesigner)));
            builder.AddCustomAttributes(typeof(GenerateSVGFromSmiles), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GenerateContent), categoryAttribute);
            builder.AddCustomAttributes(typeof(GenerateContent), new DesignerAttribute(typeof(GenerateContentDesigner)));
            builder.AddCustomAttributes(typeof(GenerateContent), new HelpKeywordAttribute(""));

            builder.AddCustomAttributes(typeof(GetResult), categoryAttribute);
            builder.AddCustomAttributes(typeof(GetResult), new DesignerAttribute(typeof(GetResultDesigner)));
            builder.AddCustomAttributes(typeof(GetResult), new HelpKeywordAttribute(""));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
