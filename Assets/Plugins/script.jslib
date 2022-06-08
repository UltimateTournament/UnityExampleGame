mergeInto(LibraryManager.library, {

  Log: function (str) {
	console.log(UTF8ToString(str));
  },

  IsSecure: function () {
	return window.location.protocol !== "http:";
  },

  Hostname: function () {
	var returnStr = window.location.hostname;
	var bufferSize = lengthBytesUTF8(returnStr) + 1;
	var buffer = _malloc(bufferSize);
	stringToUTF8(returnStr, buffer, bufferSize);
	return buffer;
  },

  Token: function () {
	var returnStr = prompt("Enter player token", "");
	var bufferSize = lengthBytesUTF8(returnStr) + 1;
	var buffer = _malloc(bufferSize);
	stringToUTF8(returnStr, buffer, bufferSize);
	return buffer;
  },

  Port: function () {
	return window.location.port * 1;
  }

});
