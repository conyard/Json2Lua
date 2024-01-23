package com.mrmao;

import cn.hutool.json.JSONArray;
import cn.hutool.json.JSONObject;
import cn.hutool.json.JSONUtil;

import java.util.Iterator;
import java.util.Map;
import java.util.Set;

/**
 * @author: MrMao
 * @Date: 2022/8/2
 **/
public class JsonConvertLuaUtil {

    /**
     * Get system new line character to adapt windows and macOS
     */
    private static final String NEW_LINE_CHARACTER = System.getProperty("line.separator");

    /**
     * Convert JsonString to Lua Table
     *
     * @param jsonObject JsonString to be converted
     * @return Lua table String
     */
    public static String toLuaTable(JSONObject jsonObject) {

        StringBuilder stringBuilder = new StringBuilder();
        //Bypass start with [return : ]
        stringBuilder.append("{").append(NEW_LINE_CHARACTER);
        //start append
        appendValue(stringBuilder, jsonObject);
        stringBuilder.append(NEW_LINE_CHARACTER).append("}");

        return stringBuilder.toString();
    }

    public static String toLuaTable(String jsonString){
        return toLuaTable(JSONUtil.parseObj(jsonString));
    }

    private static int childLevel = 1;
    private static void appendValue(StringBuilder stringBuilder, JSONObject jsonObject) {
        //Json key set
        Set<Map.Entry<String, Object>> entries = jsonObject.entrySet();

        for (Map.Entry<String, Object> entry : entries) {
            Object str = jsonObject.getObj(entry.getKey());
            if (str.toString().startsWith("[")) {
                //Append list beginning
                stringBuilder.append("  [\"").append(entry.getKey()).append("\"]")
                        .append(" = [").append(NEW_LINE_CHARACTER);
                JSONArray objects = JSONUtil.parseArray(str);
                //Start append
                Iterator<Object> iterator = objects.stream().iterator();
                while (iterator.hasNext()) {
                    appendSpace(stringBuilder, childLevel + 1);
                    //Start process Value
                    valueHandle(stringBuilder, iterator.next());
                }
                stringBuilder.deleteCharAt(stringBuilder.lastIndexOf(","));
                stringBuilder.append("  ],").append(NEW_LINE_CHARACTER);
            } else if (str.toString().startsWith("{")) {
                appendSpace(stringBuilder, childLevel);
                //Append key
                stringBuilder.append("[\"").append(entry.getKey()).append("\"]")
                        .append(" = {").append(NEW_LINE_CHARACTER);
                childLevel++;
                appendValue(stringBuilder, jsonObject.getJSONObject(entry.getKey()));
                childLevel--;
                stringBuilder.append(NEW_LINE_CHARACTER);
                appendSpace(stringBuilder, childLevel);
                stringBuilder.append("},").append(NEW_LINE_CHARACTER);
            } else {
                appendSpace(stringBuilder, childLevel);
                //Append key
                stringBuilder.append("[\"").append(entry.getKey()).append("\"]")
                        .append(" = ");
                //Start process Value
                valueHandle(stringBuilder, str);
            }
        }
        //Remove last ","
        stringBuilder.delete(stringBuilder.lastIndexOf(","), stringBuilder.length());
    }

    /**
     * According to object type to append
     *
     * @param stringBuilder Concatenated string
     * @param str           To be concatenated field
     */
    private static void valueHandle(StringBuilder stringBuilder, Object str) {
        if (str.getClass() == String.class) {
            stringBuilder.append("\"").append(str).append("\"");
        } else if (str.getClass() == Boolean.class) {
            //Boolean in lua special
            boolean booleanValue = Boolean.parseBoolean(String.valueOf(str));
            if (!booleanValue) {
                stringBuilder.append("nil");
            } else {
                stringBuilder.append("true");
            }
        } else {
            //Number dont need [""]
            stringBuilder.append(str);
        }
        stringBuilder.append(",").append(NEW_LINE_CHARACTER);
    }

    /**
     * Append space in stringBuilder
     *
     * @param stringBuilder Concatenated string
     * @param childLevel    what level
     */
    private static void appendSpace(StringBuilder stringBuilder, int childLevel) {
        //Format String
        for (int i = 0; i < childLevel; i++) {
            stringBuilder.append("  ");
        }
    }

}
