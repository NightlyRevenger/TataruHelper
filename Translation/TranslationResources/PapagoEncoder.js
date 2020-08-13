function EncodeTransaltionRequest(str)
{	
	var request=JSON.parse(str);
	request.deviceId= UidGenerator();
	e = JSON.stringify(request); 
	
	return InnerEncode(e);
}

function EncodeRequest(str)
{	
	return InnerEncode(str);
}

function InnerEncode(e)
{	
	var a = function (e) 
	{

		for (var a = e.length, t = e.length - 1; t >= 0; t--) 
		{
			var o = e.charCodeAt(t);
			( o > 127 && o <= 2047 ) ? a++ : o > 2047 && o <= 65535 && (a += 2)
			
			var tmpA=a;
		}				
		return a;

	}(e) % 6,  t = (a > 0) ? underLine(6).substr(0, 6 - a) : "";

	var tmpE2=e;
	var tmpE3=Encode("" + e + t);

	var tmpF= function (e) 
	{
		var a = underLine(1);
		var a2 = a + psubstr(e, a.charCodeAt(0) % (e.length - 2) + 1);
		return a2;
	}(tmpE3)

	return papagoReplace(tmpF)
}

function UidGenerator()
{
	var a = (new Date).getTime();
	
	
	var r ="xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (e) 
	{
		var t = (a + 16 * Math.random()) % 16 | 0;
		return a = Math.floor(a / 16), ("x" === e ? t : 3 & t | 8).toString(16)
	})
	
	return r;
}

function psubstr(e, a) {
	return e.substr(a) + e.substr(0, a)
}

function Encode(e, t) 
{
	var r = "string" != typeof e;
	
	var tmp1 = r && e.constructor === i.ArrayBuffer && (e = new Uint8Array(e));
	var tmp2 = r ? papgoStrange(e) : !t && /[^\x00-\x7F]/.test(e) ? kreanBase64(e) : btoa(e);
	
	return tmp1, tmp2
}

function kreanBase64(e) 
{	
	for (var t = "", r = 0; r < e.length; r++) 
	{
		var n = e.charCodeAt(r);
		
		n < 128 ? t += String.fromCharCode(n) : n < 2048 ? t += String.fromCharCode(192 | n >> 6) + String.fromCharCode(128 | 63 & n) : n < 55296 || n >= 57344 ? t += String.fromCharCode(224 | n >> 12) + String.fromCharCode(128 | n >> 6 & 63) + String.fromCharCode(128 | 63 & n) : (n = 65536 + ((1023 & n) << 10 | 1023 & e.charCodeAt(++r)), t += String.fromCharCode(240 | n >> 18) + String.fromCharCode(128 | n >> 12 & 63) + String.fromCharCode(128 | n >> 6 & 63) + String.fromCharCode(128 | 63 & n))
	}
	
	return btoa(t)
	
}

function papagoReplace(e) {
	return e.replace(/([a-m])|([n-z])/gi, function (e, a, t) {
		return String.fromCharCode(a ? a.charCodeAt(0) + 13 : t ? t.charCodeAt(0) - 13 : 0) || e
	})
}

function underLine(e) {
	var a="";
	for (var t = 0; t < e; t++) 
		a += String.fromCharCode(Math.floor(80 * Math.random() + 43));
	
	return a
}


function papgoStrange(e) {
	for (var t, r, n, o = "", i = e.length, s = 0, u = 3 * parseInt(i / 3); s < u;) t = e[s++], r = e[s++], n = e[s++], o += d[t >>> 2] + d[63 & (t << 4 | r >>> 4)] + d[63 & (r << 2 | n >>> 6)] + d[63 & n];
	var a = i - u;
	return 1 === a ? (t = e[s], o += d[t >>> 2] + d[t << 4 & 63] + "==") : 2 === a && (t = e[s++], r = e[s], o += d[t >>> 2] + d[63 & (t << 4 | r >>> 4)] + d[r << 2 & 63] + "="), o
}

function btoa(s) {
  var iCounter={};
  
  // String conversion as required by Web IDL.
  //s = `${s}`;
  s=s;
  // "The btoa() method must throw an "InvalidCharacterError" DOMException if
  // data contains any character whose code point is greater than U+00FF."
  for (iCounter = 0; iCounter < s.length; iCounter++) {
    if (s.charCodeAt(iCounter) > 255) {
      return null;
    }
  }
  var  out = "";
  
  for (iCounter = 0; iCounter < s.length; iCounter += 3) 
  {
    const groupsOfSix = [undefined, undefined, undefined, undefined];
    groupsOfSix[0] = s.charCodeAt(iCounter) >> 2;
    groupsOfSix[1] = (s.charCodeAt(iCounter) & 0x03) << 4;
    if (s.length > iCounter + 1) {
      groupsOfSix[1] |= s.charCodeAt(iCounter + 1) >> 4;
      groupsOfSix[2] = (s.charCodeAt(iCounter + 1) & 0x0f) << 2;
    }
    if (s.length > iCounter + 2) {
      groupsOfSix[2] |= s.charCodeAt(iCounter + 2) >> 6;
      groupsOfSix[3] = s.charCodeAt(iCounter + 2) & 0x3f;
    }
    for (var j = 0; j < groupsOfSix.length; j++) 
	{
      if (typeof groupsOfSix[j] === "undefined") {
        out += "=";
      } else {
        out += btoaLookup(groupsOfSix[j]);
      }
    }
  }
  return out;//*/
}

/**
 * Lookup table for btoa(), which converts a six-bit number into the
 * corresponding ASCII character.
 */
function btoaLookup(idx) {
  if (idx < 26) {
    return String.fromCharCode(idx + "A".charCodeAt(0));
  }
  if (idx < 52) {
    return String.fromCharCode(idx - 26 + "a".charCodeAt(0));
  }
  if (idx < 62) {
    return String.fromCharCode(idx - 52 + "0".charCodeAt(0));
  }
  if (idx === 62) {
    return "+";
  }
  if (idx === 63) {
    return "/";
  }
  // Throw INVALID_CHARACTER_ERR exception here -- won't be hit in the tests.
  return undefined;
}
