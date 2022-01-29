using System.Security.Cryptography;
 
namespace FileSignSimple
{
    class Program
    {
        static void Main(string[] args)
        {
            FileToSign s = new FileToSign("C:/123.pdf", 100000);
        }
    }
 
    class FileToSign
    {
        int _bufferSize;
        string _filePath;
        long _fileLength;
        long _blocknumber;
        object _toLock = new object();
 
        protected struct block
        {
            public long Number;
            public byte[] Data;
 
            public block(long number, byte[] data)
            {
                Number = number;
                Data = data;
            }
        }
 
        public FileToSign(string filePath, int bufferSize)
        {
            _filePath = filePath;
            _bufferSize = bufferSize;
 
            FileInfo file = new FileInfo(filePath);
            _fileLength = file.Length;
 
            int _threadsCount = Environment.ProcessorCount; ;
 
            for (int i = 0; i < _threadsCount; i++)
            {
                new Thread(new ThreadStart(DoSign)).Start();
            }
 
            Console.ReadKey();
        }
 
        private void DoSign()
        {
            while (true)
            {
                block dataBlock = GetData();
 
                if (dataBlock.Number < 0)
                    return;
 
                SHA256 sha = SHA256.Create();
 
                byte[] hash = sha.ComputeHash(dataBlock.Data);
 
                Console.Clear();
                Console.Write(dataBlock.Number + " - ");
                PrintByteArray(hash);
            }
        }
 
        public static void PrintByteArray(byte[] array)
        {
            int i;
            for (i = 0; i < array.Length; i++)
            {
                Console.Write(String.Format("{0:X2}", array[i]));
                if ((i % 4) == 3) Console.Write(" ");
            }
            Console.WriteLine("Dune");
        }
 
        protected block GetData()
        {
            block temp = new block(-1, new byte[_bufferSize]);
            long position = _blocknumber * _bufferSize;
 
            lock (_toLock)
            {
                using (BinaryReader br = new BinaryReader(File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    if (position > _fileLength)
                        return temp;
 
                    try
                    {
                        br.BaseStream.Position = position;
                        br.Read(temp.Data, 0, _bufferSize);
                        temp.Number = _blocknumber;
                        _blocknumber++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                    }
                }
 
                return temp;
            }
        }
    }
}

