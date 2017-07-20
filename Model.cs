using System;
using System.Collections.Generic;

namespace FormsByAir.SDK.Model
{
    public class TagData
    {
        public string Tag { get; set; }
        public string Data { get; set; }
    }

    public class DeliveryException
    {
        public string DocumentDeliveryId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    public class DocumentRequest
    {
        public string FormId { get; set; }
        public List<TagData> Prefill { get; set; }
        public bool Submit { get; set; }
    }

    public class DocumentRequestResponse
    {
        public string DocumentId { get; set; }
    }

    public class Document
    {
        public string DocumentId { get; set; }
        public string FormId { get; set; }
        public DateTime? RequestedDateTime { get; set; }
        public string RequestedEmailAddress { get; set; }
        public DateTime? ReceivedDateTime { get; set; }
        public DateTime? PurgedDateTime { get; set; }
        public string DocumentStatusId { get; set; }
        public string WorkflowStatusId { get; set; }
        public string Reference { get; set; }
        public string UserId { get; set; }
        public int DocumentVersion { get; set; }
        public string WorkflowUserId { get; set; }
    }

    public class Subscription
    {
        public string SubscriptionId { get; set; }
        public string Reference { get; set; }
        public string FilePath { get; set; }
    }

    public class DocumentDelivery
    {
        public string DocumentDeliveryId { get; set; }
        public string DocumentId { get; set; }
        public string SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
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
        public bool BlockSaveCookie { get; set; }
        public bool HideNavFirstSection { get; set; }
        public bool HideRestart { get; set; }
        public bool HideTitle { get; set; }
        public string ConfirmationMessage { get; set; }
        public string ClosedMessage { get; set; }
        public bool SaveCompletedSections { get; set; }
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
        public bool MatchStart { get; set; }
        public bool GetExtendedData { get; set; }
        public bool AutoCollapse { get; set; }
        public string Limit { get; set; }
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
        public string Completed { get; set; }
        public string DefaultValue { get; set; }
        public string SubscriptionId { get; set; }
        public string ValidationMethod { get; set; }
        public string ValidationMessage { get; set; }
        public bool ValidationInline { get; set; }
        public string ConfirmationMessage { get; set; }        
        public string TableId { get; set; }
        public string Audit { get; set; }
        public string ListType { get; set; }
        public Element Parent { get; set; }
        public string Format { get; set; }
        public string Min { get; set; }
        public string Max { get; set; }
        public string Step { get; set; }
        public string Decimals { get; set; }
        public string LinkedRepeater { get; set; }
        public bool Inline { get; set; }
        public bool PostSubmit { get; set; }
        public string Width { get; set; }
        public bool AttachResponse { get; set; }
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

    public class Attribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Setter { get; set; }
        public string Getter { get; set; }
        public string EntityName { get; set; }
        public string Filter { get; set; }
        public string ForEach { get; set; }
    }

    public class Entity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ForEach { get; set; }
        public string Filter { get; set; }
        public List<Attribute> Attributes { get; set; }
        public List<Entity> Entities { get; set; }
    }

    public class FileDelivery
    {
        public byte[] File { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
