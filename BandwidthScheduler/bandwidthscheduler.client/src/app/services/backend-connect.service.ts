import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { LoginController } from './controllers/LoginController';
import { AvailabilityController } from './controllers/AvailabilityController';
import { PublishController } from './controllers/schedule/PublishController';
import { RecallController } from './controllers/schedule/RecallController';
import { TeamController } from './controllers/TeamController';
import { CommitmentController } from './controllers/CommitmentController';
import { NotificationController } from './controllers/NotificationController';

@Injectable({
  providedIn: 'root',
})
export class BackendConnectService {
  private _login: LoginController;
  private _availability: AvailabilityController;
  private _commitment: CommitmentController;
  private _schedulePublish: PublishController;
  private _scheduleRecall: RecallController;
  private _team: TeamController;
  private _notification: NotificationController;

  get Login(): LoginController {
    return this._login;
  }

  get Availability(): AvailabilityController {
    return this._availability;
  }

  get Commitment(): CommitmentController {
    return this._commitment;
  }

  get SchedulePublish(): PublishController {
    return this._schedulePublish;
  }

  get ScheduleRecall(): RecallController {
    return this._scheduleRecall;
  }

  get Team(): TeamController {
    return this._team;
  }

  get Notification(): NotificationController {
    return this._notification;
  }

  constructor(http: HttpClient) {
    const baseApiUrl = 'api/';
    this._login = new LoginController(http, baseApiUrl);
    this._availability = new AvailabilityController(http, baseApiUrl);
    this._commitment = new CommitmentController(http, baseApiUrl);
    this._schedulePublish = new PublishController(http, baseApiUrl);
    this._scheduleRecall = new RecallController(http, baseApiUrl);
    this._team = new TeamController(http, baseApiUrl);
    this._notification = new NotificationController(http, baseApiUrl);
  }
}
