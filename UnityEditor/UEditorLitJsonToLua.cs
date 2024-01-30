using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using LitJson;

/// <summary>
/// 类名 : unityeditor-json数据转为lua数据
/// 作者 : Canyon / 龚阳辉
/// 日期 : 2024-01-30 14:34
/// 功能 : 
/// </summary>
public class UEditorLitJsonToLua : Editor
{
    [MenuItem("Tools_lua/_Opts/Json2Lua")]
    private static void Json2Lua()
    {
        EditorUtility.DisplayProgressBar("ConvertToLua", "Checking", 0.1f);
        string _fdir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory) + "/json_2_lua/";
        string fpdir = "Assets";
        string[] _arrs = Directory.GetFiles(fpdir, "*.json", SearchOption.AllDirectories);
        float _len = _arrs.Length;

        string fpNoAssets = Application.dataPath.Replace("Assets", "").Replace("\\", "/");

        string _it, _strCont, _strLua;
        string _fdDest, _fpDest;
        string _fname, _fnNoSuffix;
        JsonData jdRoot = null;
        for (int i = 0; i < _len; i++)
        {
            _it = _arrs[i].Replace("\\", "/");
            _fpDest = _it.Replace(fpNoAssets, "");
            _fname = Path.GetFileName(_fpDest);
            _fnNoSuffix = Path.GetFileNameWithoutExtension(_fpDest);
            _fpDest = _fpDest.Replace(_fname, _fnNoSuffix);
            // if (_it.Contains("Datas/Levels/"))
            if (_it.Contains("Datas/Levels/") || _it.Contains("Datas/Maps/"))
            {
                _fdDest = (_fdir + _fpDest).Replace(_fnNoSuffix, "");
                if (!Directory.Exists(_fdDest))
                    Directory.CreateDirectory(_fdDest);
                _fpDest = _fdir + _fpDest + ".lua";
                EditorUtility.DisplayProgressBar("ConvertToLua", _it, 0.1f + 0.8f * (i + 1) / _len);
                _strCont = File.ReadAllText(_it);
                jdRoot = JsonMapper.ToObject(_strCont);
                _strLua = ConvertLua(jdRoot);
                var bytes = System.Text.Encoding.UTF8.GetBytes(_strLua.ToString());
                System.IO.File.WriteAllBytes(_fpDest, bytes);
            }
        }
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("ConvertToLua Finished", "Finished", "Okey");
    }

    static void ConvertLuaTable(JsonData jdRoot, System.Text.StringBuilder _sbd)
    {
        if(_sbd == null)
            _sbd = new System.Text.StringBuilder();
        _sbd.Append("{");
        if (jdRoot != null)
        {
            JsonData _jsonObj = null;
            int _lens = 0;
            if (jdRoot.IsArray)
            {
                _lens = jdRoot.Count;
                for (int i = 0; i < _lens; i++)
                {
                    _jsonObj = jdRoot[i];
                    if (_jsonObj.IsArray || _jsonObj.IsObject)
                        ConvertLuaTable(_jsonObj, _sbd);
                    else
                        _sbd.Append(_jsonObj.ToJson());
                    if(i < _lens - 1)
                        _sbd.Append(",");
                }
            }
            else if(jdRoot.IsObject)
            {
                int _cnt = 0;
                _lens = jdRoot.Keys.Count;
                int _iKey = 0;
                foreach (var item in jdRoot.Keys)
                {
                    if(int.TryParse(item,out _iKey))
                        _sbd.AppendFormat("[{0}]=", _iKey);
                    else
                    {
                        // _sbd.AppendFormat("[\"{0}\"]=", item);
                        _sbd.AppendFormat("{0}=", item);
                    }
                    _jsonObj = jdRoot[item];
                    if (_jsonObj.IsArray || _jsonObj.IsObject)
                        ConvertLuaTable(_jsonObj, _sbd);
                    else
                        _sbd.Append(_jsonObj.ToJson());
                    if (_cnt < _lens - 1)
                        _sbd.Append(",");
                    _cnt = _cnt + 1;
                }
            }
            else
            {
                _sbd.Append(jdRoot.ToJson());
                _sbd.Append(",");
            }
        }
        _sbd.Append("}");
    }

    static string ConvertLua(JsonData jdRoot)
    {
        System.Text.StringBuilder _sbd = new System.Text.StringBuilder();
        _sbd.Append("return ");
        ConvertLuaTable(jdRoot, _sbd);
        string _cont = _sbd.ToString();
        _sbd.Clear();
        _sbd.Length = 0;
        return _cont;
    }
}