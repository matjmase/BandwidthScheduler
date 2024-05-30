import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IScheduleRecallRequest } from '../../../models/IScheduleRecallRequest';

export class RecallController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'schedule/recall/';
  }

  public RecallSchedule(request: IScheduleRecallRequest): Observable<void> {
    return this.http.post<void>(this._baseUrl, request);
  }
}
