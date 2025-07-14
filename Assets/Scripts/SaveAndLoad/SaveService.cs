// 新建SaveService.cs文件
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TouchSocket.Core;
using UnityEngine;

public class SaveService
{
    #region 单例模式实现
    
    // 使用Lazy实现线程安全的单例模式
    private static readonly Lazy<SaveService> _instance = new Lazy<SaveService>(() => new SaveService());
    
    /// <summary>
    /// 获取服务实例
    /// </summary>
    public static SaveService Instance => _instance.Value;
    
    #endregion
    
    #region 私有字段
    
    private string _basePath;
    private const string AESHead = "AESEncrypt";
    private const string ProductName = "SanqiGameC";
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 私有构造函数确保单例模式
    /// </summary>
    private SaveService()
    {
        InitializePlatformSpecificPaths();
        InitializeDefaultDirectories();
        InitializeDefaultPasswordFile();
    }
    
    #endregion
    
    #region 平台路径初始化
    
    private void InitializePlatformSpecificPaths()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _basePath = Application.persistentDataPath + "/";
#else
        _basePath = Application.persistentDataPath + "/";
#endif
    }
    
    #endregion
    
    #region 目录初始化
    
    private void InitializeDefaultDirectories()
    {
        string rootDir = _basePath;
        if (!Directory.Exists(rootDir))
        {
            Directory.CreateDirectory(rootDir);
        }
        
        string saveDataDir = Path.Combine(_basePath, "SaveData");
        if (!Directory.Exists(saveDataDir))
        {
            Directory.CreateDirectory(saveDataDir);
        }
    }
    
    #endregion
    
    #region 密码文件初始化
    
    private void InitializeDefaultPasswordFile()
    {
        string passwordFilePath = Path.Combine(
            _basePath, 
            $"Save_Data(游戏存在时，请勿删除)(Do not delete the game when it exists).sav");
        Debug.Log("游戏保存路径：" + _basePath);
        Debug.Log("密码文件路径：" + passwordFilePath);
        if (!File.Exists(passwordFilePath))
        {
            PlayerPrefs.DeleteKey("Password");
        }
        
        if (!PlayerPrefs.HasKey("Password"))
        {
            PlayerPrefs.SetInt("Password", 1);
            
            string saveDataDir = Path.Combine(_basePath, "SaveData");
            if (!Directory.Exists(saveDataDir))
            {
                Directory.CreateDirectory(saveDataDir);
            }
            
            using (FileStream fs = new FileStream(passwordFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] bytes = BitConverter.GetBytes(999);
                fs.Write(bytes, 0, bytes.Length);
                
                string data = GetRandomDom(16);
                bytes = FastBinaryFormatter.SerializeToBytes(data);
                
                // 先写入长度
                fs.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
                // 再写入字符串具体内容
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
            }
        }
    }
    
    #endregion
    
    #region 文件操作方法
    
    /// <summary>
    /// 删除指定路径的存档文件
    /// </summary>
    public void Delete(string path, string folder = "")
    {
        string fullPath = GetFullPath(folder, path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
    
    /// <summary>
    /// 删除指定文件夹及其所有内容
    /// </summary>
    public void DeleteFolder(string folder = "")
    {
        string fullPath = Path.Combine(_basePath, folder);
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo di = new DirectoryInfo(fullPath);
            di.Delete(true);
        }
    }
    
    /// <summary>
    /// 检查指定路径的存档是否存在
    /// </summary>
    public bool Exists(string path, string folder = "")
    {
        string fullPath = GetFullPath(folder, path);
        return File.Exists(fullPath);
    }
    
    /// <summary>
    /// 保存数据到指定路径
    /// </summary>
    public void Save<T>(T data, string path, string folder = "")
    {
        string fullPath = GetFullPath(folder, path);
        Serialize(data, fullPath);
    }
    
    /// <summary>
    /// 异步保存数据
    /// </summary>
    public System.Collections.IEnumerator SaveCoroutine<T>(T data, string path)
    {
        yield return new System.Threading.ManualResetEvent(false); // 强制让这一帧的其他操作先执行完毕
        Serialize(data, Path.Combine(_basePath, path));
        yield return new System.Threading.ManualResetEvent(false); // 确保在这一帧的所有Update调用之后执行
    }
    
    /// <summary>
    /// 从指定路径加载数据
    /// </summary>
    public T Load<T>(string path, T defaultValue, string folder = "")
    {
        string fullPath = GetFullPath(folder, path);
        
        if (File.Exists(fullPath))
        {
            return Deserialize<T>(fullPath);
        }
        
        return defaultValue;
    }
    
    #endregion
    
    #region 序列化与反序列化
    
    /// <summary>
    /// 序列化数据对象并加密存储
    /// </summary>
    public void Serialize<T>(T data, string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            // 读取字节头，判断是否已经加密过了
            byte[] headBuff = new byte[10];
            fs.Read(headBuff, 0, headBuff.Length);
            string headTag = Encoding.UTF8.GetString(headBuff);
            
            // 加密并且写入字节头
            fs.Seek(0, SeekOrigin.Begin);
            fs.SetLength(0);
            byte[] headBuffer = Encoding.UTF8.GetBytes(AESHead);
            fs.Write(headBuffer, 0, headBuffer.Length);
            
            var bytes2 =  FastBinaryFormatter.SerializeToBytes(data);
            byte[] encBuffer = AESEncrypt(bytes2, GetPassword());
            fs.Write(encBuffer, 0, encBuffer.Length);
        }
    }
    
    /// <summary>
    /// 反序列化指定路径的加密数据
    /// </summary>
    public T Deserialize<T>(string path)
    {
        T data = default(T);
        
        using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read))
        {
            byte[] headBuff = new byte[10];
            fs.Read(headBuff, 0, headBuff.Length);
            string headTag = Encoding.UTF8.GetString(headBuff);
            
            if (headTag == AESHead)
            {
                byte[] buffer = new byte[fs.Length - headBuff.Length];
                fs.Read(buffer, 0, Convert.ToInt32(fs.Length - headBuff.Length));
                
                byte[] decrypted = AESDecrypt(buffer, GetPassword());
                data = FastBinaryFormatter.Deserialize<T>(decrypted); 
            }
        }
        
        return data;
    }
    
    #endregion
    
    #region 加密解密方法
    
    /// <summary>
    /// 获取加密密码
    /// </summary>
    public string GetPassword()
    {
        try
        {  
            string passwordFilePath = Path.Combine(
                      _basePath, 
                      "Save_Data(游戏存在时，请勿删除)(Do not delete the game when it exists).sav");
                      
                  using (FileStream fs = new FileStream(passwordFilePath, FileMode.OpenOrCreate, FileAccess.Read))
                  {
                      byte[] bytes2 = new byte[4];
                      int index = fs.Read(bytes2, 0, 4);
                      int i = BitConverter.ToInt32(bytes2, 0);
                      
                      index = fs.Read(bytes2, 0, 4);
                      int length = BitConverter.ToInt32(bytes2, 0);
                      
                      bytes2 = new byte[length];
                      index = fs.Read(bytes2, 0, length);
                      
                      return FastBinaryFormatter.Deserialize<string>(bytes2);
                  }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        return "";
    }
    
    /// <summary>
    /// AES加密操作
    /// </summary>
    public static byte[] AESEncrypt(byte[] encryptByte, string encryptKey)
    {
        if (encryptByte.Length == 0) throw new Exception("明文不得为空");
        if (string.IsNullOrEmpty(encryptKey)) throw new Exception("密钥不得为空");
        
        byte[] m_strEncrypt;
        byte[] m_btIV = Convert.FromBase64String("7fJ3zPqT7L2dQn4v8w1XuA==");
        byte[] m_salt = Convert.FromBase64String("M5k9pLrT9eVnB8hN3dXy0Q==");
        
        using (Rijndael m_AESProvider = Rijndael.Create())
        {
            m_AESProvider.Padding = PaddingMode.PKCS7;
            
            using (var rdb = new Rfc2898DeriveBytes(encryptKey, m_salt))
            {
                ICryptoTransform transform = m_AESProvider.CreateEncryptor(rdb.GetBytes(32), m_btIV);
                
                using (MemoryStream m_stream = new MemoryStream())
                using (CryptoStream m_csstream = new CryptoStream(m_stream, transform, CryptoStreamMode.Write))
                {
                    m_csstream.Write(encryptByte, 0, encryptByte.Length);
                    m_csstream.FlushFinalBlock();
                    m_strEncrypt = m_stream.ToArray();
                }
            }
        }
        
        return m_strEncrypt;
    }
    
    /// <summary>
    /// AES解密操作
    /// </summary>
    public static byte[] AESDecrypt(byte[] decryptByte, string decryptKey)
    {
        if (decryptByte.Length == 0) throw new Exception("密文不得为空");
        if (string.IsNullOrEmpty(decryptKey)) throw new Exception("密钥不得为空");
        
        byte[] m_strDecrypt;
        byte[] m_btIV = Convert.FromBase64String("7fJ3zPqT7L2dQn4v8w1XuA==");
        byte[] m_salt = Convert.FromBase64String("M5k9pLrT9eVnB8hN3dXy0Q==");
        
        using (Rijndael m_AESProvider = Rijndael.Create())
        {
            m_AESProvider.Padding = PaddingMode.PKCS7;
            
            using (var rdb = new Rfc2898DeriveBytes(decryptKey, m_salt))
            {
                ICryptoTransform transform = m_AESProvider.CreateDecryptor(rdb.GetBytes(32), m_btIV);
                
                using (MemoryStream m_stream = new MemoryStream())
                using (CryptoStream m_csstream = new CryptoStream(m_stream, transform, CryptoStreamMode.Write))
                {
                    m_csstream.Write(decryptByte, 0, decryptByte.Length);
                    m_csstream.FlushFinalBlock();
                    m_strDecrypt = m_stream.ToArray();
                }
            }
        }
        
        return m_strDecrypt;
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 生成不重复随机字符串
    /// </summary>
    public static string GetRandomDom(int count)
    {
        const string t62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        long ticks = DateTime.Now.Ticks;
        string gen = "";
        int ind = 0;
        
        while (ind < count)
        {
            byte low = (byte)((ticks >> ind * 6) & 61);
            gen += t62[low];
            ind++;
        }
        
        return gen;
    }
    
    /// <summary>
    /// 构建完整文件路径
    /// </summary>
    private string GetFullPath(string folder, string path)
    {
        string saveDataDir = Path.Combine(_basePath, "SaveData");
        
        if (!string.IsNullOrEmpty(folder))
        {
            saveDataDir = Path.Combine(saveDataDir, folder);
            if (!Directory.Exists(saveDataDir))
            {
                Directory.CreateDirectory(saveDataDir);
            }
        }
        
        return Path.Combine(saveDataDir, path);
    }
    
    #endregion
}
