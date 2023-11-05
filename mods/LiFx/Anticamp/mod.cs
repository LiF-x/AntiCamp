/**
* <author>Warped Ibun</author>
* <email>lifxmod@gmail.com</email>
* <url>lifxmod.com</url>
* <credits>Christophe Roblin <christophe@roblin.no>, Warped Ibun <madbrit@co.uk></credits>
* <description>This is to stop enemies logging into your claim</description>
* <license>GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007</license>
*/

if (!isObject(LiFxAntiCamper))
{
  new ScriptObject(LiFxAntiCamper)
  {
  };
}

package LiFxAntiCamper
{
  function LiFxAntiCamper::setup() {
    LiFx::registerCallback($LiFx::hooks::onConnectCallbacks, onConnectRequest, LiFxAntiCamper);
    
  }

  function LiFxAntiCamper::onConnectRequest(%this, %client)
  {
    LiFxAntiCamper.schedule(1000, "preprocess", %client);
  }
  function LiFxAntiCamper::preprocess(%this, %client) {
    if(%client.Player.getGuildID()) {
      dbi.Select(LiFxAntiCamper, "process","SELECT " @ %client @ " as ClientID, CenterGeoID, Radius FROM `guild_lands` WHERE GuildID != " @ %client.Player.getGuildID());
    }
    else {
      dbi.Select(LiFxAntiCamper, "process","SELECT " @ %client @ " as ClientID, CenterGeoID, Radius FROM `guild_lands`");
    }
  
  }
  function LiFxAntiCamper::process(%this, %rs) {
    if(%rs.ok())
      {
        while(%rs.nextRecord()){
          %client = %rs.getFieldValue("ClientID");
          %GuildGeoID = %rs.getFieldValue("CenterGeoID");
          %Radius = %rs.getFieldValue("Radius");
          %GuildCoordinates = LiFxUtility::getPositionFromGeoID(%GuildGeoID);
          %GuildCoordinates = VectorAdd(%GuildCoordinates, "0.5 0.5 0");
          nextToken(nextToken(%GuildCoordinates, "GuildX", " "), "GuildY", " ");
          nextToken(nextToken(%client.Player.position, "PlayerX", " "), "PlayerY", " ");
          %Guild2DVec = %GuildX SPC %GuildY SPC 0;
          %Player2DVec = %PlayerX SPC %PlayerY SPC 0;
          %Direction = VectorNormalize(VectorSub(%Player2DVec, %Guild2DVec));
          if(VectorDist(%Player2DVec SPC "0", %Guild2DVec SPC "0") <= (%Radius * 2)) 
          {
            // Scale directional vector so it is scaled to the remaining distance to the full radius of the claim
            %Scaled = (%Radius * 2) - VectorDist(%Player2DVec, %Guild2DVec) + 1;
            // Add the scaled normalized vector to current player position
            %TeleportPosition = VectorAdd(%Player2DVec,VectorScale(%Direction, %Scaled));
            // Get the x and y of the new position
            nextToken(nextToken(%TeleportPosition, "TPlayerX", " "), "TPlayerY", " ");
            // find Z height at the new x and y position
            %startPoint = %TPlayerX SPC %TPlayerY SPC 1500; // Start the raycast at a high point above the terrain
            %endPoint = %TPlayerX SPC %TPlayerY SPC -1500;   // Extend the raycast downward
            %rayMask =  1 << 2;
            %rayResult = containerRayCast(%startPoint, %endPoint, %rayMask);
            // if found, tp
            if (%rayResult) {
                %hitPoint = getWords(%rayResult, 1, 3); // Get the point of intersection
                %zPosition = getWord(%hitPoint, 2);     // Get the Z position (height) of the terrain
                %teleportFinal = %TPlayerX SPC %TPlayerY SPC %zPosition;

                // Teleport
                %client.Player.setTransform(LiFxUtility::createPositionTransform(%teleportFinal));
                LiFxAntiCamper::onTeleport(%client.getCharacterId());
            }

          }
        }
      }
      dbi.remove(%rs);
      %rs.delete();
  }
  function LiFxAntiCamper::MessageAllWithCustomText(%this, %rs) {
    if(%rs.ok() && %rs.nextRecord())
    {
      LiFxUtility::messageAll(2480, %rs.getFieldValue("Message"));
    }
    dbi.remove(%rs);
    %rs.delete();
  }
  
  function LiFxAntiCamper::onTeleport(%CharID) {    
    dbi.Select(LiFxAntiCamper, "MessageAllWithCustomText", "SELECT CONCAT((SELECT CONCAT('<spop><spush><color:c6935f>', Name, ' ', LastName, '<spop><spush>') FROM `character` WHERE ID = " @ %CharID @ "),' was just moved from a claim, do not try and camp like a coward -  LiFxAnticamp') AS Message");
  }
  function LiFxAntiCamper::version() {
    return "1.0.0";
  }
};

activatePackage(LiFxAntiCamper);