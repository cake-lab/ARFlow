using UnityEngine;

using CakeLab.ARFlow.Grpc.V1;

using TMPro;

public class SessionElement : MonoBehaviour
{
    public TMP_Text infoText;

    private Session m_Session;

    public void SetSession(Session session)
    {
        m_Session = session;
        setText();
    }

    private void setText()
    {
        infoText.text =
        @$"Session <i> {m_Session.Metadata.Name} </i> \n
        Device count: {m_Session.Devices.Count}
        ";
    }
}
