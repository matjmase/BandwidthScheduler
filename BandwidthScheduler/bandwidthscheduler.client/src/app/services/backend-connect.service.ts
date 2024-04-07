import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { LoginController } from './controllers/LoginController';
import { AvailabilityController } from './controllers/AvailabilityController';
import { PublishController } from './controllers/PublishController';

@Injectable({
  providedIn: 'root',
})
export class BackendConnectService {
  private _login: LoginController;
  private _availability: AvailabilityController;
  private _publish: PublishController;

  get Login(): LoginController {
    return this._login;
  }

  get Availability(): AvailabilityController {
    return this._availability;
  }

  get Publish(): PublishController {
    return this._publish;
  }

  constructor(http: HttpClient) {
    const baseApiUrl = 'api/';
    this._login = new LoginController(http, baseApiUrl);
    this._availability = new AvailabilityController(http, baseApiUrl);
    this._publish = new PublishController(http, baseApiUrl);
  }
}
