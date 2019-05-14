using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using TikaOnDotNet.TextExtraction;

namespace MedicalEntityExtraction
{
    class Program
    {
        static string url = "http://localhost:8080/DemoServlet";
        static string format = "xml";

        static string umlsuser = [ENTER UMLS USERNAME];
        static string umlspw = [ENTER UMLS PASSWORD];

        private static string SearchServiceName = [ENTER Azure Search Service Name];
        private static string SearchServiceKey = [Enter Azure Search Service Admin API Key];
        private static string SearchServiceIndexName = "medical-tutorial";
        private static ISearchServiceClient SearchClient = new SearchServiceClient(SearchServiceName, new SearchCredentials(SearchServiceKey));
        private static ISearchIndexClient indexClient = SearchClient.Indexes.GetClient(SearchServiceIndexName);

        static TextExtractor textExtractor = new TextExtractor();

        static void Main(string[] args)
        {
            // Modify the schema of the Azure Search Index to allow new entities to be stored
            // This will include support for DiseaseDisorder, MedicationMention, SignSymptoms, 
            // AnatomicalSites broken down by terms as well as by UMLS Concepts
            UpdateIndexSchema();

            // Get the list of files to process - For demo purposes this will come from the local machine,
            // However realistically you would want to retrieve them from your Blob Storage Account
            string[] files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "data"));
            int counter = 0;
            foreach (var file in files)
            {
                counter++;
                Console.WriteLine(String.Format("Processing document: {0}", counter));
                // Extract the entities
                var medicaEntities = ProcessDoc(textExtractor.Extract(file).Text);
                // Upload the new entities to Azure Search
                UploadMedicalEntities(medicaEntities, file);

            }
        }

        static void UpdateIndexSchema()
        {
            try
            {
                var definition = new Index()
                {
                    Name = SearchServiceIndexName,
                    Fields = FieldBuilder.BuildForType<IndexSchema>()
                };
                SearchClient.Indexes.CreateOrUpdate(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Error creating index: {0}", ex.Message));
                return;
            }
        }


        static MedicalEntities ProcessDoc(string  text)
        {
            var medicalEntities = new MedicalEntities();
            medicalEntities.DiseaseDisorderList = new List<Term>();
            medicalEntities.MedicationMentionList = new List<Term>();
            medicalEntities.SignSymptomMentionList = new List<Term>();
            medicalEntities.AnatomicalSiteMentionList = new List<Term>();
            medicalEntities.DiseaseDisorderConceptList = new List<OntologyConcept>();
            medicalEntities.MedicationMentionConceptList = new List<OntologyConcept>();
            medicalEntities.SignSymptomMentionConceptList = new List<OntologyConcept>();
            medicalEntities.AnatomicalSiteMentionConceptList = new List<OntologyConcept>();
            medicalEntities.ConceptNameDictionary = new Dictionary<int, string>();

            try
            {

                var request = (HttpWebRequest)WebRequest.Create(url);

                // Take a max of X KB of text
                var subText = text.Substring(0, Math.Min(20480, text.Length));
                var postData = "q=" + subText;
                postData += "&format=" + format;
                postData += "&umlsuser=" + umlsuser;
                postData += "&umlspw=" + umlspw;
                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Timeout = 20 * 60 * 1000;   // 20 min

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (responseString != "")
                {
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(responseString);
                    string parsedText = "";
                    int begin, end;
                    Guid termId;

                    foreach (var node in xml.ChildNodes[1])
                    {
                        //Sofa 
                        if (((System.Xml.XmlElement)node).LocalName == "Sofa")
                        {
                            parsedText = ((XmlElement)node).GetAttribute("sofaString");
                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "UmlsConcept")
                        {
                            medicalEntities.ConceptNameDictionary[Convert.ToInt32(((XmlElement)node).GetAttribute("xmi:id"))] =
                                ((XmlElement)node).GetAttribute("preferredText");
                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "DiseaseDisorderMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));

                            if (!(medicalEntities.DiseaseDisorderList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.DiseaseDisorderList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower(),
                                });

                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.DiseaseDisorderConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }

                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "MedicationMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));
                            if (!(medicalEntities.MedicationMentionList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.MedicationMentionList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower()
                                });
                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.MedicationMentionConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }

                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "SignSymptomMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));
                            if (!(medicalEntities.SignSymptomMentionList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.SignSymptomMentionList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower()
                                });
                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.SignSymptomMentionConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }
                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "AnatomicalSiteMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));
                            if (!(medicalEntities.AnatomicalSiteMentionList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.AnatomicalSiteMentionList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower()
                                });
                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.AnatomicalSiteMentionConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return medicalEntities;

        }

        static void UploadMedicalEntities(MedicalEntities medicaEntities, string file)
        {
            // Upload the specified entity to Azure Search
            // Note it is much more efficient to upload content in batches

            // Convert Concept ID's to actual Concept names
            var MedicationMentionNames = new List<string>();
            var AnatomicalSiteNames = new List<string>();
            var DiseaseDisorderNames = new List<string>();
            var SignSymptomNames = new List<string>();
            foreach (var concept in medicaEntities.MedicationMentionConceptList.Select(x=>x.ontologyConcept).Distinct())
                MedicationMentionNames.Add(medicaEntities.ConceptNameDictionary.Where(x => x.Key == Convert.ToInt32(concept)).First().Value);
            foreach (var concept in medicaEntities.AnatomicalSiteMentionConceptList.Select(x => x.ontologyConcept).Distinct())
                AnatomicalSiteNames.Add(medicaEntities.ConceptNameDictionary.Where(x => x.Key == Convert.ToInt32(concept)).First().Value);
            foreach (var concept in medicaEntities.DiseaseDisorderConceptList.Select(x => x.ontologyConcept).Distinct())
                DiseaseDisorderNames.Add(medicaEntities.ConceptNameDictionary.Where(x => x.Key == Convert.ToInt32(concept)).First().Value);
            foreach (var concept in medicaEntities.SignSymptomMentionConceptList.Select(x => x.ontologyConcept).Distinct())
                SignSymptomNames.Add(medicaEntities.ConceptNameDictionary.Where(x => x.Key == Convert.ToInt32(concept)).First().Value);

            try
            {
                var uploadBatch = new List<IndexSchema>();
                var indexDoc = new IndexSchema();
                // Get the key value so that we can merge content with the existing content
                // The key is a url token encoding of the path (e.g. https://azsdemos.blob.core.windows.net/medical-tutorial/nihms637915.pdf)
                file = "https://azsdemos.blob.core.windows.net/medical-tutorial/" + file.Substring(file.LastIndexOf("\\") + 1);
                indexDoc.metadata_storage_path = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(file));
                indexDoc.medical_mentions = medicaEntities.MedicationMentionList.Select(x => x.term).Distinct().ToArray();
                indexDoc.medical_mention_concepts = MedicationMentionNames.Distinct().ToArray();
                indexDoc.sign_symptoms = medicaEntities.SignSymptomMentionList.Select(x => x.term).Distinct().ToArray();
                indexDoc.sign_symptom_concepts = SignSymptomNames.Distinct().ToArray();
                indexDoc.anatomical_sites = medicaEntities.AnatomicalSiteMentionList.Select(x => x.term).Distinct().ToArray();
                indexDoc.anatomical_site_concepts= AnatomicalSiteNames.Distinct().ToArray();
                indexDoc.disease_disorders = medicaEntities.DiseaseDisorderList.Select(x => x.term).Distinct().ToArray();
                indexDoc.disease_disorder_concepts = DiseaseDisorderNames.Distinct().ToArray();
                uploadBatch.Add(indexDoc);

                var batch = IndexBatch.MergeOrUpload(uploadBatch);

                indexClient.Documents.Index(batch);
                uploadBatch.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


    }
}
