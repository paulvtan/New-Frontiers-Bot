using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Frontiers_Bot.Controllers
{
    public class CardBuilding : MessagesController
    {

        //This card confirm the user selected name.
        public static Activity getNameCard(string userName, Activity activity) 
        {
            Activity nameConfirmCard = activity.CreateReply("Would you like to be called\n" + userName + "?");
            nameConfirmCard.Recipient = activity.From;
            nameConfirmCard.Type = "message";
            nameConfirmCard.Attachments = new List<Attachment>();

            //Build card action
            List<CardAction> cardButtons = new List<CardAction>();
            CardAction yesButton = new CardAction()
            {
                Value = "Yes, Call me " + userName,
                Type = "imBack",
                Title = "Yes"
            };

            CardAction noButton = new CardAction()
            {
                Value = "No",
                Type = "imBack",
                Title = "No"
            };
            cardButtons.Add(yesButton);
            cardButtons.Add(noButton);

            //Create thumbnailCard
            ThumbnailCard card = new ThumbnailCard()
            {
                Buttons = cardButtons
            };

            Attachment cardAttacthment = card.ToAttachment(); //Turn the card into attachment to attach to message.
            nameConfirmCard.Attachments.Add(cardAttacthment);
            return nameConfirmCard; 
        }

    }
}