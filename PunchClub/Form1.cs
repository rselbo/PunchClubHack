using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Security.Cryptography;
using System.IO;


namespace PunchClub
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
    }

    private static byte[] key = new byte[32] { 0x7b, 0xd9, 0x4f, 0x0b, 0x18, 0x02, 0x55, 0x2d, 0x72, 0xb8, 0x1b, 0x70, 0x25, 0x70, 0xde, 0xd1, 0xf1, 0x18, 0xaf, 0x90, 0xad, 0x35, 0xc4, 0x13, 0x18, 0x1a, 0x11, 0xda, 0x83, 0xec, 0x35, 0xd1 };
    private static byte[] vector = new byte[16] { 0x92, 0x40, 0xab, 0xa1, 0x02, 0x03, 0x71, 0x77, 0xe7, 0x79, 0xdd, 0x70, 0x4f, 0x20, 0x72, 0x10 };

    private void hack_Click(object sender, EventArgs e)
    {
      char[] quote = new char[1] { '\"' };
      string strMoney = money.Text.Trim(quote);
      string strSP = sp.Text.Trim(quote);
      string strStrength = strength.Text.Trim(quote);
      string strAgility = agility.Text.Trim(quote);
      string strStamina = stamina.Text.Trim(quote);

      int iMoney = 0;
      int iSP = 0;
      int iStrength = 0;
      int iAgility = 0;
      int iStamina = 0;

      if (!Int32.TryParse(strMoney, out iMoney))
      {
        MessageBox.Show(this, string.Format("The value \"{0}\" in Money is not a valid number.\nAborting hack!", strMoney));
        return;
      }
      if (!Int32.TryParse(strSP, out iSP))
      {
        MessageBox.Show(this, string.Format("The value \"{0}\" in SkillPoints is not a valid number.\nAborting hack!", strSP));
        return;
      }
      if (!Int32.TryParse(strStrength, out iStrength))
      {
        MessageBox.Show(this, string.Format("The value \"{0}\" in Strength is not a valid number.\nAborting hack!", strStrength));
        return;
      }
      if (!Int32.TryParse(strAgility, out iAgility))
      {
        MessageBox.Show(this, string.Format("The value \"{0}\" in Agility is not a valid number.\nAborting hack!", strAgility));
        return;
      }
      if (!Int32.TryParse(strStamina, out iStamina))
      {
        MessageBox.Show(this, string.Format("The value \"{0}\" in Stamina is not a valid number.\nAborting hack!", strStamina));
        return;
      }

      int saveGame = 1;
      if (save_1.Checked) saveGame = 1;
      if (save_2.Checked) saveGame = 2;
      if (save_3.Checked) saveGame = 3;

      string dataDir = string.Format("{0}Low\\Lazy Bear Games\\Punch Club", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
      if(Directory.Exists(dataDir))
      {
        Directory.SetCurrentDirectory(dataDir);
      }
      else
      {
        dataDir = Directory.GetCurrentDirectory();
      }

      string saveName = string.Format("save_{0}.dat", saveGame);
      if(!File.Exists(saveName))
      {
        MessageBox.Show(this, string.Format("Savegame {0} was not found in {1}", saveGame, dataDir));
      }

      RijndaelManaged rijndaelManaged = new RijndaelManaged();
      ICryptoTransform encryptor = rijndaelManaged.CreateEncryptor(key, vector);
      ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(key, vector);

      string basefilename = string.Format("{0}.old", saveName);
      int count = 1;
      string filename = String.Format("{0}{1}", basefilename, count);
      while (File.Exists(filename)) filename = String.Format("{0}{1}", basefilename, ++count);

      byte[] rawData = File.ReadAllBytes(saveName);

      MemoryStream memoryStream = new MemoryStream();
      CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Write);
      cryptoStream.Write(rawData, 0, rawData.Length);
      cryptoStream.FlushFinalBlock();
      byte[] data = memoryStream.ToArray();
      string strData = Encoding.Unicode.GetString(data);
      int t = strData.Length;

      //save the decrypted file - Mostly for debugging
      //File.WriteAllBytes(string.Format("{0}.dec", saveName), data);

      List<string> keys = new List<string>();
      List<string> values = new List<string>();

      //find the correct area
      int chIndex = FindIndex(data, 8, Encoding.Unicode.GetBytes("_char_res\\\":")); //Key in the ch dictionary
      ReadJsonList(data, chIndex, keys);
      if (iMoney >= 0) data = ReplaceValue(data, keys, chIndex, "_money", strMoney);
      if (iSP >= 0) data = ReplaceValue(data, keys, chIndex, "_sp", strSP);
      if (iStrength >= 0) data = ReplaceValue(data, keys, chIndex, "_str", strStrength);
      if (iAgility >= 0) data = ReplaceValue(data, keys, chIndex, "_agl", strAgility);
      if (iStamina >= 0) data = ReplaceValue(data, keys, chIndex, "_stm", strStamina);

      MemoryStream ememoryStream = new MemoryStream();
      CryptoStream ecryptoStream = new CryptoStream((Stream)ememoryStream, encryptor, CryptoStreamMode.Write);
      ecryptoStream.Write(data, 0, data.Length);
      ecryptoStream.FlushFinalBlock();
      byte[] edata = ememoryStream.ToArray();

      //save the updated decrypted file - Mostly for debugging
      //File.WriteAllBytes(string.Format("{0}.upd", saveName), data);
      File.Move(saveName, filename);
      File.WriteAllBytes(saveName, edata);

    }


    static int GetKeyIndex(byte[] data, int index)
    {
      return FindIndex(data, index, Encoding.Unicode.GetBytes("_res_type"));
    }
    static int GetValueIndex(byte[] data, int index)
    {
      return FindIndex(data, index, Encoding.Unicode.GetBytes("_res_v"));
    }

    static int FindIndex(byte[] data, int startIndex, byte[] search)
    {
      for (int i = startIndex; i < data.Length;)
      {
        int tmpI = i;
        for (int j = 0; j < search.Length; ++j) //start at 8 to skip the string header
        {
          byte a = data.ElementAt(tmpI);
          byte b = search.ElementAt(j);
          if (a == b)
          {
            ++tmpI;
          }
          else
          {
            i += j + 1; //skip the bytes searched
            break;
          }

          if (j == search.Length - 1) //full match found
          {
            return i;
          }
        }
      }
      return -1; // no match found
    }
    static void ReadJsonList(byte[] data, int chIndex, List<string> list)
    {
      byte[] comma = Encoding.Unicode.GetBytes(",");
      char[] trim = new char[4] { '\\', '\"', '[', ']' };
      int startIndex = GetKeyIndex(data, chIndex);

      int start = FindIndex(data, startIndex, Encoding.Unicode.GetBytes("["));
      int end = FindIndex(data, startIndex, Encoding.Unicode.GetBytes("]"));
      for (int i = start; i < end;)
      {
        int endword = FindIndex(data, i, comma);
        string word = Encoding.Unicode.GetString(data, i, endword - i);
        word = word.Trim(trim);
        list.Add(word);
        i = endword + 2;
      }
    }

    static byte[] ReplaceValue(byte[] data, List<string> keys, int chIndex, string key, string newValue)
    {
      byte[] comma = Encoding.Unicode.GetBytes(",");
      byte[] endbracket = Encoding.Unicode.GetBytes("]");
      int valueIndex = GetValueIndex(data, chIndex);

      int index = keys.IndexOf(key);
      if (index == -1)
      {
        byte[] insertKey = Encoding.Unicode.GetBytes(string.Format(",\\\\\\\"{0}\\\\\\\"", key));
        int keyIndex = GetKeyIndex(data, chIndex);
        int endKeyIndex = FindIndex(data, keyIndex, endbracket);
        byte[] insertValue = Encoding.Unicode.GetBytes(string.Format(",{0}", newValue));
        int endValueIndex = FindIndex(data, valueIndex, endbracket);

        byte[] updatedData = new byte[data.Length + insertKey.Length + insertKey.Length];
        int updateDataIndex = 0;
        Buffer.BlockCopy(data, 0, updatedData, updateDataIndex, endKeyIndex); //copy data up to the end of the keys 
        updateDataIndex += endKeyIndex;
        Buffer.BlockCopy(insertKey, 0, updatedData, updateDataIndex, insertKey.Length);//copy in the new key
        updateDataIndex += insertKey.Length;
        Buffer.BlockCopy(data, endKeyIndex, updatedData, updateDataIndex, endValueIndex - endKeyIndex);//copy the data from the old end to the new value start
        updateDataIndex += endValueIndex - endKeyIndex;
        Buffer.BlockCopy(insertValue, 0, updatedData, updateDataIndex, insertValue.Length); //copy in the new value
        updateDataIndex += insertValue.Length;
        Buffer.BlockCopy(data, endValueIndex, updatedData, updateDataIndex, data.Length - endValueIndex);//copy the rest of the data

        return updatedData;
      }
      else
      {
        for (int i = 0; i < index; ++i) //skip to the correct column
        {
          valueIndex = FindIndex(data, valueIndex, comma) + 2; //+2 to skip the unicode ,
        }
        int endIndex = FindIndex(data, valueIndex, comma); //find the end of the value
        int len = endIndex - valueIndex;
        byte[] value = Encoding.Unicode.GetBytes(newValue);
        byte[] updatedData = new byte[data.Length + value.Length - len];
        Buffer.BlockCopy(data, 0, updatedData, 0, valueIndex);
        Buffer.BlockCopy(value, 0, updatedData, valueIndex, value.Length);
        Buffer.BlockCopy(data, endIndex, updatedData, valueIndex + value.Length, data.Length - endIndex);
        return updatedData;
      }
    }
  }
}
