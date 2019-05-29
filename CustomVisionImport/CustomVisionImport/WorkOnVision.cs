using CustomVisionImport.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
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
            for (int i = 0; i < tagsFromExport.Count; i++)
            {
                try
                {
                    var resultHttp = await httpClient.PostAsync(urlImport + "/tags?name=" + tagsFromExport[i].Name, null);
                    if (!resultHttp.IsSuccessStatusCode)
                    {
                        Console.WriteLine(resultHttp.StatusCode + " " + resultHttp.ReasonPhrase);
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
            return true;
        }
        public async Task<bool> GetImagesTaggedFromExport()
        {
            httpClient.DefaultRequestHeaders.Remove("Training-Key");
            httpClient.DefaultRequestHeaders.Add("Training-Key", keyImport);
            imagesFromExport = new List<Image>();
            try
            {
                var numberOfImages = await httpClient.GetAsync(urlExport + "/images/tagged/count");
                var num = JsonConvert.DeserializeObject<int>(await numberOfImages.Content.ReadAsStringAsync());
                int i = 0;
                do
                {
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
                        imagesFromExport.AddRange(JsonConvert.DeserializeObject<List<Image>>(resultString));
                    }
                } while (num > 256);
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
                    Url = imagesFromExport[i].ResizedImageUri + ".jpg",
                    tagsIds = new List<string>(imagesFromExport[i].Tags.Count),
                    Regions = new List<Region>()
                };
                for (int j = 0; j < imagesFromExport[i].Tags.Count; j++)
                {
                    newImage.tagsIds.Add(imagesFromExport[i].Tags[j].TagId);
                }
                for (int j = 0; j < imagesFromExport[i].Regions.Count; j++)
                {
                    newImage.Regions.Add(new Region
                    {
                        TagId = imagesFromExport[i].Regions[j].TagId,
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
            var insertPhotos = CreateImageToImport();
            try
            {
                while (insertPhotos.Count > 64)
                {
                    var dataList = insertPhotos.GetRange(0, 64);
                    insertPhotos.RemoveRange(0, 64);
                    var dataStr = JsonConvert.SerializeObject(dataList);
                    var resultHttp = await httpClient.PostAsync(urlImport + "/images/urls", new StringContent(dataStr));
                    if (!resultHttp.IsSuccessStatusCode)
                    {
                        Console.WriteLine(resultHttp.StatusCode + " " + resultHttp.ReasonPhrase);
                        return false;
                    }
                    else
                    {
                        var resultString = await resultHttp.Content.ReadAsStringAsync();
                    }
                }
                var data = JsonConvert.SerializeObject(insertPhotos);
                var resultend = await httpClient.PostAsync(urlImport + "/images/urls", new StringContent(data));
                if (!resultend.IsSuccessStatusCode)
                {
                    Console.WriteLine(resultend.StatusCode + " " + resultend.ReasonPhrase);
                    return false;
                }
                else
                {
                    var resultString = await resultend.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
