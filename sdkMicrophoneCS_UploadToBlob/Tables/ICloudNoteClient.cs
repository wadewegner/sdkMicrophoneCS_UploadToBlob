using System;
using System.Collections.Generic;

namespace sdkMicrophoneCS.Tables
{
    public interface ICloudNoteClient
    {
        void AddCloudNote(CloudNote cloudNote, Action<CloudNoteServiceOperationResponse<bool>> callback);
    }
}
