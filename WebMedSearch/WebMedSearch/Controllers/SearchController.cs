using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using WebMedSearch.Models;


namespace WebMedSearch.Controllers
{
    // This controller contains all the calls to Azure Search.  It is important to have calls route through
    // a mid-tier due to the fact that the Azure Search key can be placed in a safe spot as opposed to having
    // it on the client side.  Also this allows greater control over things such as analytics.

    public class SearchController : Controller
    {
        private static readonly HttpClient _httpClient;
        private static readonly Uri serviceEndpoint = new Uri(Environment.GetEnvironmentVariable("SEARCH_ENDPOINT"));
        private static readonly AzureKeyCredential credential = new AzureKeyCredential(Environment.GetEnvironmentVariable("SEARCH_API_KEY"));
        private static readonly SearchIndexClient indexClient = new SearchIndexClient(serviceEndpoint, credential);
        private static readonly string indexName = "medical-tutorial";
        private readonly SearchClient IndexCounter = new SearchClient(serviceEndpoint, indexName, credential);
        // POST: Search
        [System.Web.Http.HttpPost]
        public ActionResult Docs([FromBody] QueryParameters queryParameters)
        {
            // Perform Azure Search search
            try
            {
                if (queryParameters.search == null)
                    queryParameters.search = "*";
                SearchOptions so = new SearchOptions()
                {
                    HighlightPreTag = "<b><em>",
                    HighlightPostTag = "</em></b>",
                    SearchMode = SearchMode.All,
                    Size = queryParameters.take,
                    Skip = queryParameters.skip,
                    // Add count
                    IncludeTotalCount = true,
                    QueryType = SearchQueryType.Full
                };
                foreach (var item in queryParameters.highlights)
                {
                    so.HighlightFields.Add(item);

                }
                // Limit results
                foreach (var item in queryParameters.select)
                {
                    so.HighlightFields.Add(item);

                }
                // Add facets
                foreach (var item in queryParameters.facets)
                {
                    so.HighlightFields.Add(item);

                }

                if (queryParameters.filters != null)
                {
                    string filter = String.Join(" and ", queryParameters.filters);
                    so.Filter = filter;
                }

                return Json(indexClient.GetSearchClient(indexName),

                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }



        //public ActionResult AutoComplete(string term, string code, bool fuzzy = true)
        //{
        //    // Rather than using /autocomplete, this is going against a separate index that has the distinct terms
        //    term = term + "*";
        //    SearchParameters sp = new SearchParameters()
        //    {
        //        SearchMode = SearchMode.Any,
        //        Top = 7,
        //        QueryType = QueryType.Full,
        //        SearchFields = new string[] { "label" }
        //    };
        //    try
        //    {
        //        var response = searchClient.Indexes.GetClient(termsIndexName).Documents.Search(term, sp);
        //        List<string> suggestions = new List<string>();
        //        foreach (var result in response.Results)
        //        {
        //            suggestions.Add(result.Document["label"].ToString());
        //        }

        //        return new JsonResult
        //        {
        //            JsonRequestBehavior = JsonRequestBehavior.AllowGet,
        //            Data = suggestions
        //        };
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }

        //}

        public JObject GetFDNodes(string code, string q, string nodeType)
        {
            // Calculate nodes for 3 levels

            JObject dataset = new JObject();
            int CurrentNodes = 0;

            var FDEdgeList = new List<FDGraphEdges>();
            // Create a node map that will map a facet to a node - nodemap[0] always equals the q term
            var NodeMap = new Dictionary<string, int>();
            NodeMap[q] = CurrentNodes;

            // If blank search, assume they want to search everything
            if (string.IsNullOrWhiteSpace(q))
                q = "*";

            var origTerm = string.Empty;

            var NextLevelTerms = new List<string>();

            // Apply the first level nodes
            int node = CurrentNodes;
            NodeMap[q] = node;

            // Do a query to get the 2nd level nodes
            var response = GetFacets(q, nodeType, 15);
            if (response != null)
            {
                var facetVals = (response.Facets)[nodeType];
                foreach (var facet in facetVals)
                {
                    node = -1;
                    if (NodeMap.TryGetValue(facet.ToString(), out node) == false)
                    {
                        // This is a new node
                        CurrentNodes++;
                        node = CurrentNodes;
                        NodeMap[facet.ToString()] = node;
                    }
                    // Add this facet to the fd list
                    if (NodeMap[q] != NodeMap[facet.ToString()])
                    {
                        FDEdgeList.Add(new FDGraphEdges { source = NodeMap[q], target = NodeMap[facet.ToString()] });
                        NextLevelTerms.Add(facet.ToString());
                    }
                }
            }

            // Get the 3rd level nodes by going through all the NextLevelTerms
            foreach (var term in NextLevelTerms)
            {
                response = GetFacets(q + " \"" + term + "\"", nodeType, 3);
                if (response != null)
                {
                    var facetVals = (response.Facets)[nodeType];
                    foreach (var facet in facetVals)
                    {
                        node = -1;
                        if (NodeMap.TryGetValue(facet.ToString(), out node) == false)
                        {
                            // This is a new node
                            CurrentNodes++;
                            node = CurrentNodes;
                            NodeMap[facet.ToString()] = node;
                        }
                        // Add this facet to the fd list
                        if (NodeMap[term] != NodeMap[facet.ToString()])
                        {
                            FDEdgeList.Add(new FDGraphEdges { source = NodeMap[term], target = NodeMap[facet.ToString()] });
                        }
                    }
                }

            }

            JArray nodes = new JArray();
            foreach (var entry in NodeMap)
            {
                nodes.Add(JObject.Parse("{name: \"" + entry.Key.Replace("\"", "") + "\"}"));
            }

            JArray edges = new JArray();
            foreach (var entry in FDEdgeList)
            {
                edges.Add(JObject.Parse("{source: " + entry.source + ", target: " + entry.target + "}"));
            }

            dataset.Add(new JProperty("edges", edges));
            dataset.Add(new JProperty("nodes", nodes));

            // Create the fd data object to return

            return dataset;
        }


        public SearchResults<SearchDocument> GetFacets(string searchText, string nodeType, int maxCount = 30)
        {
            // Execute search based on query string
            try
            {
                SearchOptions so = new SearchOptions()
                {
                    SearchMode = SearchMode.All,
                    Size = 0,
                    QueryType = SearchQueryType.Full
                };
                so.Select.Add("metadata_storage_path");
                so.Facets.Add(nodeType + ", count:" + maxCount);
                return indexClient.GetSearchClient(indexName).Search<SearchDocument>(searchText, so);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }


        public class FDGraphEdges
        {
            public int source { get; set; }
            public int target { get; set; }

        }

        public class AutoCompleteItem
        {
            public string id { get; set; }
            public string desc { get; set; }
        }
    }
}