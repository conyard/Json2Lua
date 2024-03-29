﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using LitJson;

public class JsonToLua : Editor
{
    // 有问题，字符串转换的时候
    [MenuItem("Tools_lua/_Opts/ConvertToLua")]
    private static void ConvertToLua()
    {
        EditorUtility.DisplayProgressBar("ConvertToLua", "Checking", 0.1f);
        string _fdir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory) + "/json_2_lua/";
        string fpdir = "Assets";
        string[] _arrs = Directory.GetFiles(fpdir, "*.json", SearchOption.AllDirectories);
        float _len = _arrs.Length;

        string fpNoAssets = Application.dataPath.Replace("Assets", "").Replace("\\", "/");

        string _it,_strCont,_strLua;
        string _fdDest, _fpDest;
        string _fname, _fnNoSuffix;
        for (int i = 0; i < _len; i++)
        {
            _it = _arrs[i].Replace("\\", "/");
            _fpDest = _it.Replace(fpNoAssets, "");
            _fname = Path.GetFileName(_fpDest);
            _fnNoSuffix = Path.GetFileNameWithoutExtension(_fpDest);
            _fpDest = _fpDest.Replace(_fname, _fnNoSuffix);
            //if (_it.Contains("Datas/Levels/") || _it.Contains("Datas/Maps/"))
            if (_it.Contains("Datas/Levels/"))
            {
                _fdDest = (_fdir + _fpDest).Replace(_fnNoSuffix,"");
                if (!Directory.Exists(_fdDest))
                    Directory.CreateDirectory(_fdDest);
                _fpDest = _fdir + _fpDest + ".lua";
                EditorUtility.DisplayProgressBar("ConvertToLua", _it, 0.1f + 0.8f*(i+1)/_len);
                _strCont = File.ReadAllText(_it);
                _strLua = ConvertLua(_strCont);
                var bytes = System.Text.Encoding.UTF8.GetBytes(_strLua.ToString());
                System.IO.File.WriteAllBytes(_fpDest, bytes);
            }
        }
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("ConvertToLua Finished", "", "Okey");
    }

    static string ConvertLua(string jsonStr)
    {
        jsonStr = jsonStr.Replace(" ", string.Empty);//去掉所有空格
        
        string lua = "return ";

        lua += ConvertJsonType(jsonStr);

        return lua;
    }

    static string ConvertJsonType(string jsonStr)
    {
        string tempStr = jsonStr.Replace("\n", "").Replace("\r", "");
        string firstChar = "";
        try
        {
            firstChar = tempStr.Substring(0, 2);
        }
        catch (System.Exception)
        {

            Debug.Log(tempStr);
        }
        
        if (firstChar == "[{")
        {
            return ConvertJsonArray(jsonStr);
        }
        else if (firstChar[0] == '{')
        {
            return ConvertJsonArray(jsonStr);
        }
        else
        {
            return ConvertJsonArrayNoKey(jsonStr);
        }
        
    }

    /// <summary>
    /// 没有key的 例如[1,2,3]
    /// </summary>
    /// <returns></returns>
    static string ConvertJsonArrayNoKey(string jsonStr)
    {
        string lastJsonStr = jsonStr.Replace("[", "{").Replace("]", "}");
        return lastJsonStr;
    }

    static string ConvertJsonArray(string jsonStr)
    {
        string lastJsonStr = "";
        var separatorIndex = jsonStr.IndexOf(':');//通过:取得所有对象
        while (separatorIndex >= 0)
        {
            separatorIndex += 1;//加上冒号
            string cutStr = jsonStr.Substring(0, separatorIndex);
            string tempKey = "";
            string tempValue = "";
            for (int i = 0; i < cutStr.Length; i++)
            {
                char c = cutStr[i];
                if (c == '[')
                {
                    c = '{';
                }
                else if (c == '"')
                {
                    continue;
                }
                else if (c == ':')
                {
                    c = '=';
                }
                tempKey += c;

            }
            jsonStr = jsonStr.Substring(separatorIndex);
            int index = 0;
            for (int i = 0; i < jsonStr.Length; i++)
            {
                
                char c = jsonStr[i];
                
                if (c == ',')
                {
                    break;
                }
                else if (c == '"')
                {
                    c = '\"';
                }
                else if (c == '{')
                {
                    //新对象的开始
                    string surplusStr = jsonStr.Substring(index);
                    int bracketNum = 0;
                    for (int j = 0; j < surplusStr.Length; j++)
                    {
                        if (surplusStr[j] == '{')
                        {
                            bracketNum++;
                        }
                        else if (surplusStr[j] == '}')
                        {
                            if (bracketNum == 1)
                            {
                                string tempStr = jsonStr.Substring(index, index + j + 1);
                                string strResult = ConvertJsonType(tempStr);
                                tempValue += strResult;
                                index = index + j;
                                break;
                            }
                            bracketNum--;
                        }
                    }
                    i = index;
                    continue;
                }
                else if (c == '[')
                {
                    string surplusStr = jsonStr.Substring(index);
                    int bracketNum = 0;
                    for (int j = 0; j < surplusStr.Length; j++)
                    {
                        if (surplusStr[j] == '[')
                        {
                            bracketNum++;
                        }
                        else if (surplusStr[j] == ']')
                        {
                            if (bracketNum == 1)
                            {
                                string tempStr = jsonStr.Substring(index, index +j + 1);
                                string strResult = ConvertJsonType(tempStr);
                                tempValue += strResult;
                                index = index + j;
                                break;
                            }
                            bracketNum--;
                        }
                    }
                    i = index;
                    continue;
                }
                else if (c == ']')
                {
                    c = '}';
                }
                index = i;
                tempValue += c;
            }
            lastJsonStr += tempKey + tempValue;
            jsonStr = jsonStr.Substring(index+1);
            separatorIndex = jsonStr.IndexOf(':');
        }
        return lastJsonStr;
    }
}