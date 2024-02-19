using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using LitJson;

/// <summary>
/// 类名 : 项目工程-地图-关卡-json数据转为lua数据
/// 作者 : Canyon / 龚阳辉
/// 日期 : 2024-01-30 15:51
/// 功能 : 
/// </summary>
public class ProjLevMapJsonToLua : Editor
{
    static public void Empty(DirectoryInfo direInfo)
    {
        foreach (var file in direInfo.GetFiles())
            file.Delete();
        foreach (var subDireInfo in direInfo.GetDirectories())
            subDireInfo.Delete(true);
    }

    [MenuItem("Map/ProjLevMapJson2Lua(地图-关卡-json-转-lua)",false,50)]
    private static void ProjLevMapJson2Lua()
    {
        EditorUtility.DisplayProgressBar("ProjLevMapJson2Lua", "Checking", 0.1f);
        string fpNoAssets = Application.dataPath.Replace("Assets", "").Replace("\\", "/");
        string _fdir = fpNoAssets;

        EditorUtility.DisplayProgressBar("ProjLevMapJson2Lua", "Checking", 0.2f);
        string fpdir = "Assets"; // "Assets/Lua/Datas"
        string[] _arrs = Directory.GetFiles(fpdir, "*.json", SearchOption.AllDirectories);
        float _len = _arrs.Length;
        string _it, _strCont, _strLua;
        string _fdDest, _fpDest;
        string _fname, _fnNoSuffix;
        JsonData jdRoot = null;
        bool _isLev = false;
        string _fpDestTileMeta = null;
        List<string> _tileKes = null;
        bool _isClearLev = false, _isClearMap = false;
        for (int i = 0; i < _len; i++)
        {
            _it = _arrs[i].Replace("\\", "/");
            _fpDest = _it.Replace(fpNoAssets, "");
            _fname = Path.GetFileName(_fpDest);
            _fnNoSuffix = Path.GetFileNameWithoutExtension(_fpDest);
            _fpDest = _fpDest.Replace(_fname, _fnNoSuffix);
            _isLev = _it.Contains("Datas/Levels/");
            if (_isLev || _it.Contains("Datas/Maps/"))
            {
                _fdDest = (_fdir + _fpDest).Replace(_fnNoSuffix, "");
                if (!Directory.Exists(_fdDest))
                    Directory.CreateDirectory(_fdDest);
                _fpDest = _fdir + _fpDest + ".lua";
                if (_isLev)
                    _fpDest = _fpDest.Replace("Levels/", "Levels_Lua/");
                else
                    _fpDest = _fpDest.Replace("Maps/", "Maps_Lua/");
                if(_isLev && !_isClearLev)
                {
                    _isClearLev = true;
                    _fdDest = _fpDest.Replace(_fnNoSuffix, "").Replace(".lua", "");
                    if (Directory.Exists(_fdDest))
                    {
                        var dInfo = new DirectoryInfo(_fdDest);
                        Empty(dInfo);
                    }else
                        Directory.CreateDirectory(_fdDest);
                }

                if (!_isLev && !_isClearMap)
                {
                    _isClearMap = true;
                    _fdDest = _fpDest.Replace(_fnNoSuffix, "").Replace(".lua", "");
                    if (Directory.Exists(_fdDest))
                    {
                        var dInfo = new DirectoryInfo(_fdDest);
                        Empty(dInfo);
                    }
                    else
                        Directory.CreateDirectory(_fdDest);
                }
                EditorUtility.DisplayProgressBar("ProjLevMapJson2Lua", _it, 0.2f + 0.7f * (i + 1) / _len);
                _strCont = File.ReadAllText(_it);
                jdRoot = JsonMapper.ToObject(_strCont);
                if(_isLev)
                    _strLua = ConvertLua4Lev(jdRoot);
                else
                {
                    if (_tileKes == null)
                    {
                        _fpDestTileMeta = _fpDest.Replace(_fnNoSuffix, "TD_TileMeta");
                        if(!File.Exists(_fpDestTileMeta))
                        {
                            _strLua = ConvertLua4MapTileMeta(jdRoot,ref _tileKes);
                            System.IO.File.WriteAllText(_fpDestTileMeta, _strLua);
                        }
                    }
                    if (_tileKes == null || _tileKes.Count == 0)
                        continue;
                    _strLua = ConvertLua4Map(jdRoot, _tileKes);
                }
                var bytes = System.Text.Encoding.UTF8.GetBytes(_strLua.ToString());
                System.IO.File.WriteAllBytes(_fpDest, bytes);
            }
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("ProjLevMapJson2Lua", "Finished", "Okey");
    }

    static void ConvertLuaTable(JsonData jdRoot, System.Text.StringBuilder _sbd, bool isEmpty2Nil = false, bool isEndTable = true)
    {
        if (jdRoot == null)
            return;
        if (_sbd == null)
            _sbd = new System.Text.StringBuilder();
        bool isArray = jdRoot.IsArray;
        bool isObject = jdRoot.IsObject;
        int _lens = isArray ? jdRoot.Count : (isObject ? jdRoot.Keys.Count : 0);
        if (isEndTable && _lens == 0)
        {
            _sbd.Append("nil");
            return;
        }
        _sbd.Append("{");
        JsonData _jsonObj = null;
        if (jdRoot.IsArray)
        {
            _lens = jdRoot.Count;
            for (int i = 0; i < _lens; i++)
            {
                _jsonObj = jdRoot[i];
                if (_jsonObj.IsArray || _jsonObj.IsObject)
                    ConvertLuaTable(_jsonObj, _sbd, isEmpty2Nil, true);
                else
                    _sbd.Append(_jsonObj.ToJson());
                if (i < _lens - 1)
                    _sbd.Append(",");
            }
        }
        else if (jdRoot.IsObject)
        {
            int _cnt = 0;
            _lens = jdRoot.Keys.Count;
            int _iKey = 0;
            foreach (var item in jdRoot.Keys)
            {
                if (int.TryParse(item, out _iKey))
                    _sbd.AppendFormat("[{0}]=", _iKey);
                else
                {
                    // _sbd.AppendFormat("[\"{0}\"]=", item);
                    _sbd.AppendFormat("{0}=", item);
                }
                _jsonObj = jdRoot[item];
                if (_jsonObj.IsArray || _jsonObj.IsObject)
                    ConvertLuaTable(_jsonObj, _sbd, isEmpty2Nil, true);
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
        if (isEndTable)
            _sbd.Append("}");
    }

    static string ConvertLua4Lev(JsonData jdRoot)
    {
        System.Text.StringBuilder _sbd = new System.Text.StringBuilder();
        _sbd.Append("return ");
        ConvertLuaTable(jdRoot, _sbd);
        string _cont = _sbd.ToString();
        _sbd.Clear();
        _sbd.Length = 0;
        return _cont;
    }

    static JsonData GetJOne(JsonData jdRoot)
    {
        if (jdRoot.IsArray)
            return jdRoot[0];
        else if(jdRoot.IsObject)
        {
            foreach (var key in jdRoot.Keys)
            {
                return jdRoot[key];
            }
        }
        return jdRoot;
    }

    static string ConvertLua4MapTileMeta(JsonData jdRoot,ref List<string> _tileKes)
    {
        JsonData jdTileOne = GetJOne(jdRoot["tiles"]);
        if(_tileKes == null)
            _tileKes = new List<string>(jdTileOne.Keys);

        System.Text.StringBuilder _sbd = new System.Text.StringBuilder();
        _sbd.Append("local _tdTileKeys = {");
        int _lens = _tileKes.Count;
        int _cnt = 0;
        foreach (var item in _tileKes)
        {
            _sbd.AppendFormat("\"{0}\"", item);
            if (_cnt < _lens - 1)
                _sbd.Append(",");
            _cnt = _cnt + 1;
        }
        _sbd.Append("}").AppendLine();

        _sbd.AppendLine("local _funcIndexTile = function(t,k)");
        _sbd.Append("\t").AppendLine("local _index = nil");
        _sbd.Append("\t").AppendLine("for i = 1,#_tdTileKeys do");
        _sbd.Append("\t\t").AppendLine("if k == _tdTileKeys[i] then");
        _sbd.Append("\t\t\t").AppendLine("_index = i");
        _sbd.Append("\t\t\t").AppendLine("break");
        _sbd.Append("\t\t").AppendLine("end");
        _sbd.Append("\t").AppendLine("end");
        _sbd.Append("\t").AppendLine("return _index and t[_index] or nil");
        _sbd.AppendLine("end");
        _sbd.Append("return { __index = _funcIndexTile}");
        string _cont = _sbd.ToString();
        _sbd.Clear();
        _sbd.Length = 0;
        return _cont;
    }

    static string ConvertLua4Map(JsonData jdRoot, List<string> _tileKes)
    {
        JsonData jdTiles = jdRoot["tiles"];
        JsonData jdTileOne = GetJOne(jdTiles);
        jdRoot.Remove("tiles");
        System.Text.StringBuilder _sbd = new System.Text.StringBuilder();
        _sbd.Append("return ");
        ConvertLuaTable(jdRoot, _sbd,true,false);
        _sbd.Append(",tiles=");
        ConvertLuaTable4MapTile(jdTiles, _sbd,_tileKes);
        _sbd.Append("}");
        string _cont = _sbd.ToString();
        _sbd.Clear();
        _sbd.Length = 0;
        return _cont;
    }

    static void ConvertLuaTable4MapTile(JsonData jdRoot, System.Text.StringBuilder _sbd, List<string> listKey)
    {
        if (jdRoot == null)
            return;
        if (_sbd == null)
            _sbd = new System.Text.StringBuilder();
        bool isArray = jdRoot.IsArray;
        bool isObject = jdRoot.IsObject;
        int _lens = isArray ? jdRoot.Count : (isObject ?jdRoot.Keys.Count : 0);
        if (_lens == 0)
        {
            _sbd.Append("nil");
            return;
        }

        _sbd.Append("{");
        int _cnt = 0;
        _lens = jdRoot.Keys.Count;
        int _iKey = 0;
        JsonData _jsonObj = null;
        JsonData _jItem = null;
        foreach (var item in jdRoot.Keys)
        {
            _jsonObj = jdRoot[item];
            if (_jsonObj.IsObject)
            {
                if (_jsonObj.Keys.Count == 0)
                    continue;
            }
            if (int.TryParse(item, out _iKey))
                _sbd.AppendFormat("[{0}]={{", _iKey);
            else
            {
                // _sbd.AppendFormat("[\"{0}\"]=", item);
                _sbd.AppendFormat("{0}={{", item);
            }
            for (int i = 0; i < listKey.Count; i++)
            {
                _jItem = _jsonObj[listKey[i]];
                if (_jItem.IsArray || _jItem.IsObject)
                    ConvertLuaTable(_jItem, _sbd, true);
                else
                    _sbd.AppendFormat("{0}", _jItem.ToJson());
                if(i < listKey.Count - 1)
                    _sbd.Append(",");
            }
            _sbd.Append("}");
            if (_cnt < _lens - 1)
                _sbd.Append(",");
            _cnt = _cnt + 1;
        }
        _sbd.Append("}");
    }
}