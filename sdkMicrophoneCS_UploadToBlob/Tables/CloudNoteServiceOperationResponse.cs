using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace sdkMicrophoneCS.Tables
{
    public class CloudNoteServiceOperationResponse<T>
    {
        public CloudNoteServiceOperationResponse(bool success, HttpStatusCode statusCode, T content, string errorMessage)
        {
            this.Success = success;
            this.StatusCode = statusCode;
            this.Content = content;
            this.ErrorMessage = errorMessage;
        }

        public bool Success { get; private set; }

        public HttpStatusCode StatusCode { get; private set; }

        public T Content { get; private set; }

        public string ErrorMessage { get; private set; }
    }
}
