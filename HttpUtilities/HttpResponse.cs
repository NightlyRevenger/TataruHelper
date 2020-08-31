using System;
using System.Collections.Generic;
using System.Text;

namespace HttpUtilities
{
    public class HttpResponse
    {
        public virtual string Body { get; protected set; }

        public virtual bool IsSuccessful { get; protected set; }

        public virtual Exception InnerException { get; protected set; }

        public HttpResponse()
        {
            this.Body = null;
            this.IsSuccessful = false;
            this.InnerException = null;
        }

        public HttpResponse(bool isSuccessful, string bodey)
        {
            this.Body = bodey;
            this.IsSuccessful = isSuccessful;
            this.InnerException = null;
        }

        public HttpResponse(bool isSuccessful, string bodey, Exception exception)
        {
            this.Body = bodey;
            this.IsSuccessful = isSuccessful;
            this.InnerException = exception;
        }
    }
}
