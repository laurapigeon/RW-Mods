using Fisobs.Properties;

namespace Inchworms;

sealed class InchwormProperties : ItemProperties
{
    private readonly Inchworm _inchworm;

    public InchwormProperties(Inchworm inchworm)
    {
        _inchworm = inchworm;
    }

    public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
    {
        if (_inchworm.State.alive)
        {
            grabability = Player.ObjectGrabability.CantGrab;
        }
        else
        {
            grabability = Player.ObjectGrabability.BigOneHand;
        }
    }
}