using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;
using Piggy;

namespace PigSwitch.Hubs 
{
    public partial class AppHub 
    {

        [HubMethodName("ss")]
        public async Task SaveSettings(FormPost<UserSettings> post)
        {
            //FormPost<UserSettings> post = await req.Content.ReadAsAsync<FormPost<UserSettings>>();
            using (BrokerDatabase context = new BrokerDatabase())
            {
                FormResponse r = new FormResponse();

                BrokerUser user = context.BrokerUsers.FirstOrDefault(a => a.Email == post.ChromeEmail);
                if (user != null)
                {
                    user.LiveID = post.Data.LiveID;

                    if (post.Data.LiveKey != "dummy")
                    {
                        user.LiveKey = post.Data.LiveKey;
                    }

                    user.LiveTurnedOn = post.Data.LiveTurnedOn;
                    user.Mobile = post.Data.Mobile;
                    user.TestID = post.Data.TestID;

                    if (post.Data.TestKey != "dummy")
                    {
                        user.TestKey = post.Data.TestKey;
                    }

                    user.DateTimeUpdated = DateTime.UtcNow;

                    int changed = await context.SaveChangesAsync();
                    r.Success = changed > 0;

                    //Update In-Memory
                    var model = ActiveModels.FirstOrDefault(a => a.Key == user.ID);
                    if (model.Value != null)
                    {
                        model.Value.OneSignalID = user.OneSignalID;
                        model.Value.Email = user.Email;
                        model.Value.LiveTurnedOn = user.LiveTurnedOn == true;
                        model.Value.KeyId = model.Value.LiveTurnedOn ? user.LiveID : user.TestID;
                        model.Value.KeySecret = model.Value.LiveTurnedOn ? user.LiveKey : user.TestKey;
                    }
                }
                else
                {
                    r.Message = $"Error: {post.ChromeEmail} not found!";
                } 
            }

            await SendModel();
        }


        [HubMethodName("sx")]
        public async Task SavePreferences(FormPost<BrokerPreference> post)
        {
            //FormPost<BrokerPreference> post = await req.Content.ReadAsAsync<FormPost<BrokerPreference>>();

            using (BrokerDatabase context = new BrokerDatabase())
            {
                FormResponse r = new FormResponse();

                BrokerPreference obj = context.BrokerPreferences.FirstOrDefault(a => a.FK_UserID == post.ID);
                if (obj == null)
                {
                    obj = new BrokerPreference();
                    obj.FK_UserID = post.ID;
                    context.BrokerPreferences.Add(obj);
                }

                obj.EnableLong = post.Data.EnableLong;
                obj.EnableShort = post.Data.EnableShort;
                obj.Quantity = post.Data.Quantity;
                obj.EnableMarket = post.Data.EnableMarket;
                obj.LimitOptionId = post.Data.LimitOptionId;
                obj.EnableAutoClose = post.Data.EnableAutoClose;
                obj.EnablePush = post.Data.EnablePush;
                obj.EnableEmail = post.Data.EnableEmail;
                obj.EnableSound = post.Data.EnableSound;

                obj.EnableTake = post.Data.EnableTake;
                obj.TakeAt = post.Data.TakeAt;
                obj.EnableStop = post.Data.EnableStop;
                obj.StopAt = post.Data.StopAt;
                obj.EnableSame = post.Data.EnableSame;
                obj.MaxSame = post.Data.MaxSame;
                obj.EnableGreed = post.Data.EnableGreed;

                obj.EnablePeg = post.Data.EnablePeg;
                obj.PegAt = post.Data.PegAt;

                obj.EnableSecondTake = post.Data.EnableSecondTake;
                obj.SecondTakeAt = post.Data.SecondTakeAt;
                obj.EnableGel = post.Data.EnableGel;
                obj.GelBy = post.Data.GelBy;
                obj.EnableMultiply = post.Data.EnableMultiply;
                obj.MultiplyFactor = post.Data.MultiplyFactor;
                obj.EnableResetOnChop = post.Data.EnableResetOnChop;
                obj.EnableRiskyFlips = post.Data.EnableRiskyFlips;

                obj.DateTimeUpdated = DateTime.UtcNow;

                //context.Configuration.ValidateOnSaveEnabled = false;
                int changed = await context.SaveChangesAsync();
                r.Success = changed > 0;

                //Update In-Memory
                var model = ActiveModels.FirstOrDefault(a => a.Key == post.ID);
                if (model.Value != null)
                {
                    model.Value.Preferences = obj;
                }
            }

            await SendModel();
        }

    }
}