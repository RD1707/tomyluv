namespace Alpheratz;

public partial class Level1 : Node2D
{
	[Export]
	SpecialArea leftArea;
	[Export]
	LynxCamera cam;
	[Export]
	InteractiveMessage rest;
	[Export]
	PlatformerPlayer player;
	[Export]
	PlatformerPlayer npc2;
	[Export]
	Area2D npc2_Dialogue;
	[Export]
	PlatformerPlayer npc3;
	[Export]
	Area2D npc3_Dialogue;
	[Export]
	Area2D nextLevelPortal;
	[Export]
	HUD hud;
	[Export]
	CanvasLayer pauseMenu;
	[Export]
	CutsceneMode cutsceneMode;

	public int maxHearts = 3;
	public int currentHearts = 3;

	public int secreteAreaFoundCounter = 0;
	
	public int coins = 0;
	public int timeRemain = 289;

	public override void _Ready()
	{
		if (GameWorld.Instance.HasMeta("max_hearts"))
		{
			maxHearts = (int)GameWorld.Instance.GetMeta("max_hearts");
			currentHearts = maxHearts;
		}

		GameWorld.Instance.PlayBGM("res://sounds/bgm_farm_town.mp3");

		GameWorld.Instance.Connect(
			nameof(GameWorld.SignalName.CoinCollected),
			Callable.From(OnCoinCollected)
		);

		GameWorld.Instance.Connect(
			nameof(GameWorld.SignalName.SecreteAreaFound), 
			Callable.From(onSecreteAreaFound)
		);

		GameWorld.Instance.Connect(
			nameof(GameWorld.SignalName.PlayerHurt), 
			Callable.From(OnPlayerHurt)
		);

		bool leftAreaFirstTime = false;

		leftArea.BodyEntered += (Node2D body) => {
			cam.LimitLeft -= 1000;
			if (!leftAreaFirstTime)
			{
				leftAreaFirstTime = true;
				player.chatBubble.PlayText("Será que eu esqueci algo\nem casa?");
			}
		};

		leftArea.BodyExited += (Node2D body) => {
			cam.LimitLeft += 1000;
		};

		InteractiveMessage.MessageActivatedEventHandler onRest = null;
		onRest = () => {
			GD.Print("[INFO] REST");
			GameWorld.Instance.PlayBGM("", 0);
			GameWorld.Instance.ChangeScene("res://scenes/rest_scene.tscn");
			rest.MessageActivated -= onRest;
		};
		rest.MessageActivated += onRest;

		CreateTween().TweenCallback(Callable.From(() => {
			hud.SetHearts(currentHearts, maxHearts);
			hud.collectedCoins.SetNumber(coins);
			hud.remainTime.SetNumber(timeRemain);
		})).SetDelay(0.1f);

		CreateTween().SetLoops().TweenCallback(Callable.From(() => {
			timeRemain -= 1;
			hud.remainTime.SetNumber(timeRemain);
			if (timeRemain <= 0)
			{
				GameWorld.Instance.ChangeScene("res://scenes/rest_scene.tscn");
			}
		})).SetDelay(1.0f);

		nextLevelPortal.BodyEntered += (Node2D body) => {
			GameWorld.Instance.ChangeScene("res://scenes/level_2_intro.tscn");
		};

		CreateTween().TweenCallback(Callable.From(() => {
			player.chatBubble.PlayText("Eu tenho que ir logo!\nmeu namorado está me esperando!");
		})).SetDelay(2.0f);

		SetupNPC2();
		SetupNPC3();
	}

	private void OnPlayerHurt()
	{
		currentHearts -= 1;
		hud.SetHearts(currentHearts, maxHearts);
		if (currentHearts <= 0)
		{
			GameWorld.Instance.PlaySFX("res://sounds/sfx_sounds_damage3.wav");
			player.collisionShape2D.SetDeferred("disabled", true);
			CreateTween().TweenCallback(Callable.From(()=>{
				GameWorld.Instance.ChangeScene("res://scenes/game_over_scene.tscn");
			})).SetDelay(1.0f);
		}
	}

	private void SetupNPC2()
	{
		int npc2Moves = 0;
		string npc2State = "idle";

		npc2_Dialogue.BodyEntered += (Node2D body) => {
			cam.target = npc2;
			npc2.SetMeta("Zoom", Vector2.One * 3.5f);

			cutsceneMode.EnableCutscene();

			npc2State = "talking";
			Tween tween = CreateTween();

			CutsceneMode.CutsceneSkippedEventHandler onCutsceneSkipped = null;
			onCutsceneSkipped = () => {
				tween.Kill();
				cam.target = player;
				cutsceneMode.CutsceneSkipped -= onCutsceneSkipped;
				cutsceneMode.DisableCutscene();
			};
			cutsceneMode.CutsceneSkipped += onCutsceneSkipped;

			tween.TweenCallback(Callable.From(() => {
				npc2.chatBubble.PlayText("Bom dia, Manu! ^_^");
			})).SetDelay(0.5f);
			tween.TweenCallback(Callable.From(() => {
				npc2.chatBubble.PlayText("Voce está indo em direcao a aventuda de novo?");
			})).SetDelay(2.0f);
			tween.TweenCallback(Callable.From(() => {
				npc2.chatBubble.PlayText("Eu posso te acompanhar.");
			})).SetDelay(2.0f);
			tween.TweenCallback(Callable.From(() => {
				npc2.chatBubble.PlayText("mas eu estou um pouco assustado com as\ncriaturas... :(");
			})).SetDelay(2.0f);
			tween.TweenCallback(Callable.From(() => {
				npc2.chatBubble.PlayText("Mudando de assunto, boa sorte! ^ ^");
			})).SetDelay(2.8f);
			tween.TweenCallback(Callable.From(() => {
				player.chatBubble.PlayText("Obrigado, Mr. Mochi.\nTenha um bom dia!");
			})).SetDelay(2.0f);
			tween.TweenCallback(Callable.From(() => {
				npc2State = "idle";
				// Release camera focus
				cam.target = player;

				cutsceneMode.DisableCutscene();
				cutsceneMode.CutsceneSkipped -= onCutsceneSkipped;
			})).SetDelay(2.0f);
			// destroy the observer so it becomes a one-shot event handler
			npc2_Dialogue.QueueFree();
			npc2_Dialogue = null;
		};

		CreateTween().SetLoops().TweenCallback(Callable.From(() => {
			if (npc2State == "idle")
			{
				float rd = GD.Randf();
				if (rd < 0.1 && npc2Moves > -3)
				{
					npc2.x_input = -1;
					npc2Moves -= 1;
				}
				else if (rd < 0.2 && npc2Moves < 2)
				{
					npc2.x_input = 1;
					npc2Moves += 1;
				}
				else
				{
					npc2.x_input = 0;
				}
			}
			else if (npc2State == "talking")
			{
				// Flip to face player
				npc2.x_input = 0;
				bool flip = (player.Position.X - npc2.Position.X) <= 0;
				npc2.animatedSprite.FlipH = flip;
			}

		})).SetDelay(0.5f);
	}

	private void SetupNPC3()
	{
		string npc3State = "";
		npc3_Dialogue.BodyEntered += (Node2D body) => {
			npc3_Dialogue.QueueFree();
			npc3_Dialogue = null;
			Tween tween = CreateTween();
			tween.TweenCallback(Callable.From(() => {
				npc3.chatBubble.PlayText("Oi, Manu! ^_^");
			})).SetDelay(0.5f);
			tween.TweenCallback(Callable.From(() => {
				npc3.chatBubble.PlayText("Eu te esperei por tanto tempo!");
			})).SetDelay(2.0f);
			tween.TweenCallback(Callable.From(() => {
				npc3.chatBubble.PlayText("Finalmente, você chegou!");
			})).SetDelay(2.0f);
			if (secreteAreaFoundCounter == 6)
			{
				tween.TweenCallback(Callable.From(() => {
					npc3.chatBubble.PlayText("wow, voce encontrou todas as areas secretas!");
				})).SetDelay(2.0f);
				tween.TweenCallback(Callable.From(() => {
					npc3.emoBubble.PlayEmo(2);
				})).SetDelay(2.0f);
				tween.TweenCallback(Callable.From(() => {
					npc3.chatBubble.PlayText("Estou orgulhoso de voce! ^_^");
				})).SetDelay(2.0f);
				tween.TweenCallback(Callable.From(() => {
					player.chatBubble.PlayText("Que nada, foi apenas sorte!");
				})).SetDelay(2.0f);
			}
			tween.TweenCallback(Callable.From(() => {
				// walk right
				npc3State = "walking";
				npc3.x_input = 1;
				npc3.emoBubble.PlayEmo(3);
			})).SetDelay(1.0f);
		};
		CreateTween().SetLoops().TweenCallback(Callable.From(() => {
			if (npc3State != "walking")
			{
				npc3.x_input = 0;
				bool flip = (player.Position.X - npc3.Position.X) <= 0;
				npc3.animatedSprite.FlipH = flip;
			}
		})).SetDelay(0.5f);
	}

	private void OnCoinCollected()
	{
		coins += 1;
		hud.collectedCoins.SetNumber(coins);
	}

	private void onSecreteAreaFound()
	{
		GD.Print("Area secreta encontrada!");
		GameWorld.Instance.PlaySFX("res://sounds/sfx_sounds_fanfare1.wav");
		player.emoBubble.PlayEmo(2);
		secreteAreaFoundCounter += 1;
	}
}
