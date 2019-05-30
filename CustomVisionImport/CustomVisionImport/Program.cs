using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CustomVisionImport
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the export url Custom vision with project id");
            var urlExport = Console.ReadLine();
            Console.WriteLine("Enter the export training key Custom vision");
            var keyExport = Console.ReadLine();
            Console.WriteLine("Enter the import url Custom vision with project id");
            var urlImport = Console.ReadLine();
            Console.WriteLine("Enter the import key Custom vision");
            var keyImport = Console.ReadLine();
            WorkOnVision workOnVision = new WorkOnVision(urlExport, keyExport, urlImport, keyImport);
            if (await workOnVision.GetTagsFromExport())
                if (await workOnVision.AddTagsToImport())
                    if(await workOnVision.GetImagesTaggedFromExport())
                        if(await workOnVision.InsertImagesIntoImport())
                            await workOnVision.TrainTheModelImport();
        }
    }
}
