// State of the Azure Search facets and filters will be stored here
var searchParameters = [];
// This will contain the current search results passed from the controller
var searchResults = [];
// This will contain the facet filters and those facets selected
var facetTypes = { facets: [], selected: [] };

// Intialize the current state of the search parameters
searchParameters["search"] = "";
searchParameters["skip"] = "0";
searchParameters["take"] = "10";
searchParameters["select"] = ['metadata_storage_path', 'metadata_storage_name', 'metadata_storage_last_modified', 'content', 'anatomical_sites', 'anatomical_site_concepts',
    'sign_symptoms', 'sign_symptom_concepts', 'disease_disorders', 'disease_disorder_concepts',
    'medical_mentions', 'medical_mention_concepts', 'people', 'organizations'];
searchParameters["highlights"] = ['content'];
searchParameters["facets"] = ['anatomical_sites,count:10', 'anatomical_site_concepts,count:10',
    'sign_symptoms,count:10', 'sign_symptom_concepts,count:10', 'disease_disorders,count:10', 'disease_disorder_concepts,count:10',
    'medical_mentions,count:10', 'medical_mention_concepts,count:10', 'people, count:10', 'organizations, count:10'];
searchParameters["filters"] = [];

