using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Communicator
{
    public interface IEmailSender
    {
        public void SendEmail();
    }

    public class EmailSender : IEmailSender
    {
        public void SendEmail()
        {
            Console.WriteLine("Email being sent...!");
        }
    }
}
