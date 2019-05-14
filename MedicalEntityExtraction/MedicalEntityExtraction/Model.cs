using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalEntityExtraction
{
    [SerializePropertyNamesAsCamelCase]
    public partial class IndexSchema
    {
        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.StandardLucene)]
        public string content { get; set; }

        public string metadata_storage_content_type { get; set; }

        [IsFilterable, IsFacetable]
        public Int64 metadata_storage_size { get; set; }

        [IsFilterable, IsFacetable]
        public DateTimeOffset? metadata_storage_last_modified { get; set; }

        public string metadata_storage_content_md5 { get; set; }

        [IsFilterable]
        public string metadata_storage_name { get; set; }

        [System.ComponentModel.DataAnnotations.Key]
        [IsFilterable]
        public string metadata_storage_path { get; set; }

        public string metadata_content_type { get; set; }
        public string metadata_language { get; set; }
        public string metadata_title { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] people { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] organizations { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] locations { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] keyphrases { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string language { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] medical_mentions { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] medical_mention_concepts { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] disease_disorders { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] disease_disorder_concepts { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] sign_symptoms { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] sign_symptom_concepts { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] anatomical_sites { get; set; }

        [IsSearchable, IsFilterable, IsFacetable]
        public string[] anatomical_site_concepts { get; set; }

    }

    public class MedicalEntities
    {
        public List<Term> DiseaseDisorderList { get; set; }
        public List<Term> MedicationMentionList { get; set; }
        public List<Term> SignSymptomMentionList { get; set; }
        public List<Term> AnatomicalSiteMentionList { get; set; }

        public List<OntologyConcept> DiseaseDisorderConceptList { get; set; }
        public List<OntologyConcept> MedicationMentionConceptList { get; set; }
        public List<OntologyConcept> SignSymptomMentionConceptList { get; set; }
        public List<OntologyConcept> AnatomicalSiteMentionConceptList { get; set; }

        public Dictionary<int, string> ConceptNameDictionary { get; set; }
    }

    public class Concept
    {
        public int ConceptId { get; set; }
        public string ConceptName { get; set; }
    }

    public class Term
    {
        public Guid termId { get; set; }
        public string term { get; set; }
    }
    public class OntologyConcept
    {
        public Guid termId { get; set; }
        public Guid conceptId { get; set; }
        public string ontologyConcept { get; set; }
    }

}
