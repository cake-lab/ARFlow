using UnityEngine;

using CakeLab.ARFlow.Grpc.V1;

using TMPro;
using UnityEngine.UI;

public class SessionElement : MonoBehaviour
{
    public Button selectButton;
    public TMP_Text infoText;

    private Session m_Session;
    public Session session
    {
        get => m_Session;
        set
        {
            m_Session = value;
            setText();
        }
    }

    private void setText()
    {
        infoText.text =
        @$"Session <i> {m_Session.Metadata.Name} </i> \n
        Device count: {m_Session.Devices.Count}
        ";
    }
}
