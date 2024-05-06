import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IScheduleProposalRequest } from '../../models/IScheduleProposalRequest';
import { IScheduleProposalResponse } from '../../models/IScheduleProposalResponse';
import { IScheduleSubmitRequest } from '../../models/IScheduleSubmitRequest';

export class PublishController {
  private _baseUrl: string;

  constructor(private http: HttpClient, baseApiUrl: string) {
    this._baseUrl = baseApiUrl + 'publish/';
  }

  public RequestScheduleTimes(
    request: IScheduleProposalRequest
  ): Observable<IScheduleProposalResponse> {
    return this.http.post<IScheduleProposalResponse>(
      this._baseUrl + 'proposal',
      request
    );
  }

  public SubmitSchedule(request: IScheduleSubmitRequest): Observable<void> {
    return this.http.post<void>(this._baseUrl + 'submit', request);
  }
}
