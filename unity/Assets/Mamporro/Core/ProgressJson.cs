using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Mamporro.Core.Progress
{
    public sealed class ProgressJsonException:Exception
    {
        public readonly string Path,Code;
        public ProgressJsonException(string path,string code):base(code){Path=path;Code=code;}
    }

    // Conserva el token numérico: un double redondeado no demuestra que la
    // cantidad externa fuera un entero exacto (p. ej. 1.00000000000000001).
    public sealed class ProgressNumber
    {
        public readonly string Token;
        public readonly double Value;
        internal ProgressNumber(string token,double value){Token=token;Value=value;}
        public bool TryInteger(int max,out int value)
        {
            value=0;string text=Token;bool negative=text[0]=='-';if(negative)text=text.Substring(1);
            int e=text.IndexOfAny(new[]{'e','E'}),exponent=0;
            string mantissa=e<0?text:text.Substring(0,e);
            int dot=mantissa.IndexOf('.'),fraction=dot<0?0:mantissa.Length-dot-1;
            string digits=mantissa.Replace(".","").TrimStart('0');
            if(digits.Length==0)return true;
            if(negative||e>=0&&!int.TryParse(text.Substring(e+1),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out exponent))return false;
            long scale=(long)exponent-fraction;
            int end=digits.Length;while(end>0&&digits[end-1]=='0'){end--;scale++;}
            if(scale<0||end+scale>10)return false;
            long result=0;for(int i=0;i<end;i++)result=result*10+digits[i]-'0';
            for(long i=0;i<scale;i++)result*=10;
            if(result>max)return false;value=(int)result;return true;
        }
    }

    // JSON estricto y acotado, sin IO ni dependencia del deserializador Unity.
    public static class ProgressJson
    {
        public const int MaxBytes=262144,MaxDepth=16,MaxNodes=4096,MaxEntries=128,MaxString=1024,MaxId=64;
        static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        public static object Parse(byte[] bytes)
        {
            if(bytes==null||bytes.Length>MaxBytes)throw new ProgressJsonException("/","input.limit");
            try{return Parse(Utf8.GetString(bytes));}
            catch(DecoderFallbackException){throw new ProgressJsonException("/","input.encoding");}
        }
        public static object Parse(string text)
        {
            if(text==null||text.Length>MaxBytes)throw new ProgressJsonException("/","input.limit");
            try{if(Utf8.GetByteCount(text)>MaxBytes)throw new ProgressJsonException("/","input.limit");}
            catch(EncoderFallbackException){throw new ProgressJsonException("/","input.encoding");}
            var parser=new Parser(text);return parser.Read();
        }
        public static string Pointer(string key)=>key.Replace("~","~0").Replace("/","~1");
        sealed class Parser
        {
            readonly string text;int position,nodes;
            public Parser(string text){this.text=text;if(text.Length>0&&text[0]=='\ufeff')position=1;}
            bool Is(char c)=>position<text.Length&&text[position]==c;
            void Space(){while(position<text.Length&&(text[position]==' '||text[position]=='\r'||text[position]=='\n'||text[position]=='\t'))position++;}
            static void Error(string path,string code="input.invalid"){throw new ProgressJsonException(path,code);}
            void Expect(char c,string path){if(!Is(c))Error(path);position++;}
            public object Read(){var value=Value(1,"");Space();if(position!=text.Length)Error("/");return value;}
            object Value(int depth,string path)
            {
                if(depth>MaxDepth||++nodes>MaxNodes)Error(path,"input.limit");Space();
                if(position>=text.Length)Error(path);
                char c=text[position];
                if(c=='{'){
                    position++;Space();var result=new Dictionary<string,object>(StringComparer.Ordinal);
                    if(Is('}')){position++;return result;}
                    while(true){
                        if(result.Count>=MaxEntries)Error(path,"input.limit");
                        string key=String(path);Space();Expect(':',path);
                        string child=path+"/"+Pointer(key);if(result.ContainsKey(key))Error(child,"input.invalid");
                        result.Add(key,Value(depth+1,child));Space();if(Is('}')){position++;return result;}
                        Expect(',',path);Space();
                    }
                }
                if(c=='['){
                    position++;Space();var result=new List<object>();if(Is(']')){position++;return result;}
                    while(true){
                        if(result.Count>=MaxEntries)Error(path,"input.limit");
                        result.Add(Value(depth+1,path+"/"+result.Count));Space();if(Is(']')){position++;return result;}
                        Expect(',',path);Space();
                    }
                }
                if(c=='"')return String(path);
                if(c=='t'){Literal("true",path);return true;}if(c=='f'){Literal("false",path);return false;}
                if(c=='n'){Literal("null",path);return null;}
                if(c=='-'||c>='0'&&c<='9')return Number(path);
                Error(path);return null;
            }
            void Literal(string value,string path)
            {
                if(text.Length-position<value.Length||string.CompareOrdinal(text,position,value,0,value.Length)!=0)Error(path);
                position+=value.Length;
            }
            string String(string path)
            {
                Expect('"',path);var result=new StringBuilder();
                while(position<text.Length){
                    char c=text[position++];if(c=='"'){
                        string value=result.ToString();try{Utf8.GetByteCount(value);}catch(EncoderFallbackException){Error(path,"input.encoding");}
                        return value;
                    }
                    if(c<32)Error(path);
                    if(c=='\\'){
                        if(position>=text.Length)Error(path);c=text[position++];
                        switch(c){case '"':case '\\':case '/':break;
                            case 'b':c='\b';break;case 'f':c='\f';break;case 'n':c='\n';break;case 'r':c='\r';break;case 't':c='\t';break;
                            case 'u':
                                if(position+4>text.Length)Error(path);int hex=0;
                                for(int i=0;i<4;i++){
                                    char h=text[position++];int digit=h>='0'&&h<='9'?h-'0':h>='a'&&h<='f'?h-'a'+10:h>='A'&&h<='F'?h-'A'+10:-1;
                                    if(digit<0)Error(path);hex=hex*16+digit;
                                }
                                c=(char)hex;break;
                            default:Error(path);break;
                        }
                    }
                    if(result.Length>=MaxString)Error(path,"input.limit");result.Append(c);
                }
                Error(path);return null;
            }
            bool Digit()=>position<text.Length&&text[position]>='0'&&text[position]<='9';
            ProgressNumber Number(string path)
            {
                int start=position;if(Is('-'))position++;
                if(Is('0'))position++;else{if(!Digit())Error(path);while(Digit())position++;}
                if(Is('.')){position++;if(!Digit())Error(path);while(Digit())position++;}
                if(Is('e')||Is('E')){position++;if(Is('+')||Is('-'))position++;if(!Digit())Error(path);while(Digit())position++;}
                string token=text.Substring(start,position-start);
                if(!double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out double value)||double.IsNaN(value)||double.IsInfinity(value))Error(path,"value.invalid");
                return new ProgressNumber(token,value);
            }
        }

        // Escritura canónica de un árbol propio: claves ordenadas y cultura invariante.
        // No acepta tipos arbitrarios ni objetos con reflexión.
        public static string Stringify(object value)
        {
            var result=new StringBuilder();Write(result,value);return result.ToString();
        }
        static void Quote(StringBuilder result,string text)
        {
            result.Append('"');foreach(char c in text){switch(c){
                case '"':result.Append("\\\"");break;case '\\':result.Append("\\\\");break;
                default:if(c<32)result.Append("\\u").Append(((int)c).ToString("x4",CultureInfo.InvariantCulture));else result.Append(c);break;
            }}result.Append('"');
        }
        static void Write(StringBuilder result,object value)
        {
            if(value==null){result.Append("null");return;}
            if(value is string text){Quote(result,text);return;}
            if(value is bool boolean){result.Append(boolean?"true":"false");return;}
            if(value is ProgressNumber number){result.Append(number.Token);return;}
            if(value is int integer){result.Append(integer.ToString(CultureInfo.InvariantCulture));return;}
            if(value is double real){
                if(double.IsNaN(real)||double.IsInfinity(real))throw new ArgumentException("Número no finito");
                result.Append(real.ToString("R",CultureInfo.InvariantCulture));return;
            }
            if(value is Dictionary<string,object> map){
                result.Append('{');var keys=new List<string>(map.Keys);keys.Sort(StringComparer.Ordinal);bool comma=false;
                foreach(var key in keys){if(comma)result.Append(',');comma=true;Quote(result,key);result.Append(':');Write(result,map[key]);}
                result.Append('}');return;
            }
            if(value is IList list){result.Append('[');for(int i=0;i<list.Count;i++){if(i>0)result.Append(',');Write(result,list[i]);}result.Append(']');return;}
            throw new ArgumentException("Tipo JSON no admitido");
        }
    }
}
