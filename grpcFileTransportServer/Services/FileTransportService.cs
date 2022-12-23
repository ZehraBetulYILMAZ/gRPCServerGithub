using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using grpcFileServer;
using System.Threading;

namespace grpcFileTransportServer.Services
{
    public class FileTransportService:FileService.FileServiceBase
    {
        readonly IWebHostEnvironment webHostEnvironment;
        public FileTransportService(IWebHostEnvironment _webHostEnvironment)
        {
            webHostEnvironment = _webHostEnvironment;
        }

        public override async Task<Empty> FileUpload(IAsyncStreamReader<BytesContent> requestStream, ServerCallContext context)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "files");
             if(!Directory.Exists(path)) 
                Directory.CreateDirectory(path);

            FileStream fileStream = null;
            try
            {
                int count = 0;
                decimal chunksize = 0;
                while (await requestStream.MoveNext())
                {
                    if (count++ == 0)
                    {
                        fileStream = new FileStream($"{path}/{requestStream.Current.Info.FileNmae}{requestStream.Current.Info.FileExtension}", FileMode.CreateNew);
                        fileStream.SetLength(requestStream.Current.FileSize);
                    }

                    var buffer = requestStream.Current.Buffer.ToByteArray();
                    await fileStream.WriteAsync(buffer, 0, buffer.Length);
                    Console.WriteLine($"{Math.Round(((chunksize += requestStream.Current.ReadedByte) * 100) / requestStream.Current.FileSize)} %");
                }
            }
            catch 
            {

            }


            await fileStream.DisposeAsync();
            fileStream.Close();

            return new Empty();

        }
        public override async Task FileDownload(grpcFileServer.FileInfo request, IServerStreamWriter<BytesContent> responseStream, ServerCallContext context)
        {
            string path = Path.Combine(webHostEnvironment.WebRootPath, "files");
            using  FileStream fileStream = new FileStream($"{path}/{request.FileNmae}{request.FileExtension}", FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[2048];
            BytesContent content = new BytesContent
            {
                FileSize = fileStream.Length,
                Info = new grpcFileServer.FileInfo { FileNmae = Path.GetFileNameWithoutExtension(fileStream.Name), FileExtension = Path.GetExtension(fileStream.Name) },
                ReadedByte = 0
            };
            while ((content.ReadedByte = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                content.Buffer = ByteString.CopyFrom(buffer);
                await responseStream.WriteAsync(content);
            }
            fileStream.Close();
        }

    }
}
