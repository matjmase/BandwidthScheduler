import { HttpClient } from '@angular/common/http';

export class RecallController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'schedule/recall/';
  }
}
