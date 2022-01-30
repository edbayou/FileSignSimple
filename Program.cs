using System.Security.Cryptography;


class Program
{
    static void Main(string[] args)
    {
        string fileName; 
        int blockSize;
        GetFileNameAndBlockSize(out fileName, out blockSize);
        FileToSign s = new FileToSign(fileName, blockSize);

    }
    static void GetFileNameAndBlockSize(out string fileName, out int blockSize)
		{
			GetParam:
			fileName = "";
			blockSize = 0;
			Console.WriteLine("Enter file:");
			string _fileName = Console.ReadLine();
			Console.WriteLine("Enter block size:");
			string _blockSize = Console.ReadLine();
			try{
				if(File.Exists(_fileName)){
					fileName = _fileName;
				}
				else {
					throw new ArgumentException("File is not exist");
				}
				
				if(int.TryParse(_blockSize,out int newBlockSize)) {
					if (newBlockSize <= 0){
						throw new ArgumentException("Block Size should be more then 0");
					}
					blockSize = newBlockSize;
				}
				else{
					throw new ArgumentException("Block Size should be number");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				goto GetParam;
			}
		}

}

class FileToSign
{
    int _bufferSize;
    string _filePath;
    long _fileLength;
    long _blocknumber;
    object _toLock = new object();

    protected struct Block
    {
        public long Number;
        public byte[] Data;

        public Block(long number, byte[] data)
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
            Thread myThread = new Thread(new ThreadStart(DoSign));
            myThread.Name = "Поток " + i.ToString();
            myThread.Start();
        }
    }

    private void DoSign()
    {
        while (true)
        {
            Block dataBlock = GetData();

            if (dataBlock.Number < 0)
                return;

            SHA256 sha = SHA256.Create();

            byte[] hash = sha.ComputeHash(dataBlock.Data);
            string hashToString = Convert.ToBase64String(
               hash);
            Console.WriteLine(Thread.CurrentThread.Name + ": " + dataBlock.Number + " - " + hashToString);
        }
    }
    protected Block GetData()
    {
        lock (_toLock)
        {
            Block block = new Block(-1, new byte[_bufferSize]);
            long position = _blocknumber * _bufferSize;
            using (BinaryReader br = new BinaryReader(File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                if (position > _fileLength)
                    return block;

                try
                {
                    br.BaseStream.Position = position;
                    br.Read(block.Data, 0, _bufferSize);
                    block.Number = _blocknumber;
                    _blocknumber++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            }

            return block;
        }
    }
}
