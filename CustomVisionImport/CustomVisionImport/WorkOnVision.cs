using CustomVisionImport.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomVisionImport
{
    class WorkOnVision
    {
        private readonly HttpClient httpClient;
        private readonly string urlExport;
        private readonly string keyExport;
        private readonly string urlImport;
        private readonly string keyImport;
        private List<Tag> tagsFromExport;
        private List<Tag> tagsFromImport;
        private List<Image> imagesFromExport;
        public WorkOnVision(string urlExport, string keyExport, string urlImport, string keyImport)
        {
            httpClient = new HttpClient();
            this.urlExport = urlExport;
            this.keyExport = keyExport;
            this.urlImport = urlImport;
            this.keyImport = keyImport;
        }
        public async Task<bool> GetTagsFromExport()
        {
            httpClient.DefaultRequestHeaders.Remove("Training-Key");
            httpClient.DefaultRequestHeaders.Add("Training-Key", keyExport);
            Console.WriteLine("retrieve tags from export model");
            try
            {
                var resultHttp = await httpClient.GetAsync(urlExport + "/tags");
                if (!resultHttp.IsSuccessStatusCode)
                {
                    Console.WriteLine(resultHttp.StatusCode + " " + resultHttp.ReasonPhrase);
                    return false;
                }
                else
                {
                    var resultString = await resultHttp.Content.ReadAsStringAsync();
                    tagsFromExport = JsonConvert.DeserializeObject<List<Tag>>(resultString);
                    Console.WriteLine(tagsFromExport.Count + " tags found on the export model");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public async Task<bool> AddTagsToImport()
        {
            httpClient.DefaultRequestHeaders.Remove("Training-Key");
            httpClient.DefaultRequestHeaders.Add("Training-Key", keyImport);
            tagsFromImport = new List<Tag>(tagsFromExport.Count);
            Console.WriteLine("Importing tags to the import model");
            for (int i = 0; i < tagsFromExport.Count; i++)
            {
                try
                {
                    var resultHttp = await httpClient.PostAsync(urlImport + "/tags?name=" + tagsFromExport[i].Name, null);
                    if (!resultHttp.IsSuccessStatusCode)
                    {
                        Console.WriteLine(resultHttp.StatusCode + " " + resultHttp.ReasonPhrase);
                        return false;
                    }
                    else
                    {
                        var resultString = await resultHttp.Content.ReadAsStringAsync();
                        tagsFromImport.Add(JsonConvert.DeserializeObject<Tag>(resultString));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            Console.WriteLine("Tags imported successfully");
            return true;
        }
        public async Task<bool> GetImagesTaggedFromExport()
        {
            httpClient.DefaultRequestHeaders.Remove("Training-Key");
            httpClient.DefaultRequestHeaders.Add("Training-Key", keyExport);
            imagesFromExport = new List<Image>();
            Console.WriteLine("Retrieve tagged images from the export model");
            try
            {
                var numberOfImages = await httpClient.GetAsync(urlExport + "/images/tagged/count");
                var num = JsonConvert.DeserializeObject<int>(await numberOfImages.Content.ReadAsStringAsync());
                var i = 0;
                do
                {
                    num -= imagesFromExport.Count;
                    var resultHttp = await httpClient.GetAsync(urlExport + "/images/tagged?take=256&skip=" + i * 256);
                    i++;
                    if (!resultHttp.IsSuccessStatusCode)
                    {
                        Console.WriteLine(resultHttp.StatusCode + " " + resultHttp.ReasonPhrase);
                        return false;
                    }
                    else
                    {
                        var resultString = await resultHttp.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<Image>>(resultString);
                        imagesFromExport.AddRange(data);
                    }
                    //Thread.Sleep(3000);
                } while (num > 256);
                Console.WriteLine(imagesFromExport.Count + " images retrieve from the export model");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
        public List<Image> CreateImageToImport()
        {
            var insertPhotos = new List<Image>(imagesFromExport.Count);
            for (int i = 0; i < imagesFromExport.Count; i++)
            {
                var newImage = new Image
                {
                    Url = imagesFromExport[i].ResizedImageUri,
                    tagsIds = new List<string>(imagesFromExport[i].Tags.Count),
                    Regions = new List<Region>()
                };
                for (int j = 0; j < imagesFromExport[i].Tags.Count; j++)
                {
                    newImage.tagsIds.Add(tagsFromImport.FirstOrDefault(t => t.Name == imagesFromExport[i].Tags[j].TagName)?.Id);
                }
                for (int j = 0; j < imagesFromExport[i].Regions.Count; j++)
                {
                    newImage.Regions.Add(new Region
                    {
                        TagId = tagsFromImport.FirstOrDefault(t => t.Name == imagesFromExport[i].Regions[j].Tagname)?.Id,
                        Left = imagesFromExport[i].Regions[j].Left,
                        Top = imagesFromExport[i].Regions[j].Top,
                        Width = imagesFromExport[i].Regions[j].Width,
                        Height = imagesFromExport[i].Regions[j].Height
                    });
                }
                insertPhotos.Add(newImage);
            }
            return insertPhotos;
        }
        public async Task<bool> InsertImagesIntoImport()
        {
            httpClient.DefaultRequestHeaders.Remove("Training-Key");
            httpClient.DefaultRequestHeaders.Add("Training-Key", keyImport);
            var insertPhotos = CreateImageToImport();
            Console.WriteLine("Importing images to the import model");
            try
            {
                while (insertPhotos.Count > 64)
                {
                    var dataList = insertPhotos.GetRange(0, 64);
                    insertPhotos.RemoveRange(0, 64);
                    var dataStr = JsonConvert.SerializeObject(new SendImages
                    {
                        Images = dataList
                    },
                        Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                    var resultHttp = await httpClient.PostAsync(urlImport + "/images/urls", new StringContent(dataStr, Encoding.UTF8, "application/json"));
                    if (!resultHttp.IsSuccessStatusCode)
                    {
                        Console.WriteLine(resultHttp.StatusCode + " " + resultHttp.ReasonPhrase);
                        return false;
                    }
                }
                var data = JsonConvert.SerializeObject(new SendImages
                {
                    Images = insertPhotos
                },
                        Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                var resultend = await httpClient.PostAsync(urlImport + "/images/urls", new StringContent(data, Encoding.UTF8, "application/json"));
                if (!resultend.IsSuccessStatusCode)
                {
                    Console.WriteLine(resultend.StatusCode + " " + resultend.ReasonPhrase);
                    return false;
                }
                Console.WriteLine(imagesFromExport.Count + " of images imported to the import model");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
        public async Task<bool> TrainTheModelImport()
        {
            httpClient.DefaultRequestHeaders.Remove("Training-Key");
            httpClient.DefaultRequestHeaders.Add("Training-Key", keyImport);
            Console.WriteLine("Training Model in progress... Go on customvision.ai to see the result");
            var resultHttp = await httpClient.PostAsync(urlImport + "/train", null);
            if (!resultHttp.IsSuccessStatusCode)
            {
                Console.WriteLine(resultHttp.StatusCode + " " + resultHttp.ReasonPhrase);
                return false;
            }
            else
            {
                var resString = await resultHttp.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<TrainReturn>(resString);
                Console.WriteLine(res);
                return true;
            }
        }
    }
}
