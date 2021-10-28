using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SendGridManager
{
    public class TemplateResult
    {
        [JsonPropertyName("templates")]
        public List<TemplateInfo> Templates { get; set; }
    }

    public class TemplateInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonPropertyName("versions")]
        public List<TemplateVersionInfo> Versions { get; set; }

        public TemplateVersionInfo ActiveVersion => Versions?.FirstOrDefault(v => v.IsActive);
    }

    public class TemplateVersionInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("active")]
        public int Active { get; set; }

        public bool IsActive => Active == 1;

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("plain_content")]
        public string PlainContent { get; set; }

        [JsonPropertyName("html_content")]
        public string HtmlContent { get; set; }
    }

    public class CreateTemplate
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("generation")]
        public string Generation => "dynamic";
    }

    public class CreateVersion
    {
        [JsonPropertyName("active")]
        public int Active => 1;

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("html_content")]
        public string HtmlContent { get; set; }

        [JsonPropertyName("plain_content")]
        public string PlainContent { get; set; }

        [JsonPropertyName("generate_plain_content")]
        public bool GeneratePlainContent => true;

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("editor")]
        public string Editor => "design";
    }
}

