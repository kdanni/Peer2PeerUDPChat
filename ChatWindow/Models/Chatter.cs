using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2PeerChat.Models
{
    public class Chatter
    {
        public static readonly string Anonymous = "Anonymous";
        
        private string _nick;
        private bool _isAnon;

        public bool ThisIsMe { get; set; }

        public bool IsAnonymous { get { return _isAnon; } }

        public string Nick
        {
            get { return _nick; }
            set {
                _nick = value;
                if (Anonymous.Equals(value))
                {
                    _isAnon = true;
                }
            }
        }

        public Chatter()
        {
            _nick = Anonymous;
            _isAnon = true;
            ThisIsMe = false;
        }

        public Chatter(string nickName)
        {
            Nick = nickName;
            ThisIsMe = false;
        }

        public Chatter(string nickName, bool thisIsMeBro)
        {
            Nick = nickName;
            ThisIsMe = thisIsMeBro;
        }
    }
}
