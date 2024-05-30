import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { LoginController } from './controllers/LoginController';
import { AvailabilityController } from './controllers/AvailabilityController';
import { StaffController } from './controllers/StaffController';
import { PublishController } from './controllers/schedule/PublishController';
import { ScheduleController } from './controllers/schedule/ScheduleController';
import { RecallController } from './controllers/schedule/RecallController';

@Injectable({
  providedIn: 'root',
})
export class BackendConnectService {
  private _login: LoginController;
  private _availability: AvailabilityController;
  private _schedule: ScheduleController;
  private _schedulePublish: PublishController;
  private _scheduleRecall: RecallController;
  private _staff: StaffController;

  get Login(): LoginController {
    return this._login;
  }

  get Availability(): AvailabilityController {
    return this._availability;
  }

  get Schedule(): ScheduleController {
    return this._schedule;
  }

  get SchedulePublish(): PublishController {
    return this._schedulePublish;
  }

  get ScheduleRecall(): RecallController {
    return this._scheduleRecall;
  }

  get Staff(): StaffController {
    return this._staff;
  }

  constructor(http: HttpClient) {
    const baseApiUrl = 'api/';
    this._login = new LoginController(http, baseApiUrl);
    this._availability = new AvailabilityController(http, baseApiUrl);
    this._schedule = new ScheduleController(http, baseApiUrl);
    this._schedulePublish = new PublishController(http, baseApiUrl);
    this._scheduleRecall = new RecallController(http, baseApiUrl);
    this._staff = new StaffController(http, baseApiUrl);
  }
}
