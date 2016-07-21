using System.Collections.Generic;

namespace FormsByAir.SDK.Model
{
    public class TagData
    {
        public string Tag { get; set; }
        public string Data { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class DocumentRequest
    {
        public string FormId { get; set; }
        public List<TagData> Prefill { get; set; }
    }

    public class DocumentRequestResponse
    {
        public string DocumentId { get; set; }        
    }

    public class DocumentDelivery
    {
        public string DocumentDeliveryId { get; set; }
        public string DocumentId { get; set; }
    }

    public class DocumentResponse
    {
        public Schema Schema { get; set; }
    }

    public class Schema
    {
        public Element Form { get; set; }        
        public List<SimpleType> SimpleTypes { get; set; }
        public List<ComplexType> ComplexTypes { get; set; }
        public string FormId { get; set; }
        public int Version { get; set; }
        public string Style { get; set; }
        public string ValidationScript { get; set; }
        public string TrackingScript { get; set; }
        public string DocumentReference { get; set; }
        public bool BlockSave { get; set; }
    }

    public class Element
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public bool AllowMany { get; set; }
        public bool Hidden { get; set; }
        public bool ReadOnly { get; set; }
        public bool PopupNote { get; set; }
        public bool ForceDropdown { get; set; }     //deprecated
        public bool AllowManualEntry { get; set; }
        public int? Limit { get; set; }
        public string Title { get; set; }
        public string Prompt { get; set; }
        public string Note { get; set; }
        public string Hint { get; set; }
        public string AutofillKey { get; set; }
        public string Visibility { get; set; }
        public List<Element> Elements { get; set; }
        public List<Element> DocumentElements { get; set; }
        public SimpleType SimpleType { get; set; }
        public ElementType ElementType { get; set; }
        public string DocumentValue { get; set; }
        public string DefaultValue { get; set; }
        public string SubscriptionId { get; set; }
        public string ValidationMethod { get; set; }
        public string ValidationMessage { get; set; }
        public string ConfirmationMessage { get; set; }
        public string TableId { get; set; }
        public string Audit { get; set; }
        public string ListType { get; set; }
    }

    public enum ElementType
    {
        Question = 0,
        Group = 1,
        Condition = 3,
        Workflow = 4,
        Section = 5
    }

    public class Enumeration
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class SimpleType
    {
        public string Name { get; set; }
        public List<Enumeration> Values { get; set; }
    }

    public class ComplexType
    {
        public string Name { get; set; }
        public List<Element> Elements { get; set; }
    }
}
