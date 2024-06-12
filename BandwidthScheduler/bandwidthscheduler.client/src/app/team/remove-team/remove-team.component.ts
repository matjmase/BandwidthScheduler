import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { ITeam } from '../../models/db/ITeam';
import { TeamSelectorComponent } from '../../commonControls/team-selector/team-selector.component';
import { BackendConnectService } from '../../services/backend-connect.service';
import { StandardSnackbarService } from '../../services/standard-snackbar.service';
import { HttpErrorResponse } from '@angular/common/http';
import { TeamSelectorContainerComponent } from '../TeamSelectorContainerComponent';
import { SpinnerCardHorizontalStretch } from '../../commonControls/spinner-card/spinner-card.component';
import { NotificationUpdateService } from '../../services/notification-update.service';
import { TeamSelectorType } from '../../commonControls/team-selector/team-selector-type';

@Component({
  selector: 'app-remove-team',
  templateUrl: './remove-team.component.html',
  styleUrl: './remove-team.component.scss',
})
export class RemoveTeamComponent extends TeamSelectorContainerComponent {
  public TeamType: TeamSelectorType = TeamSelectorType.Active;

  constructor(
    private messageSnackBar: StandardSnackbarService,
    private backend: BackendConnectService,
    private notificationChange: NotificationUpdateService
  ) {
    super();
  }

  protected override ResetTeamSelector(): void {}
  public override GetHorizontalStretch(): SpinnerCardHorizontalStretch {
    return SpinnerCardHorizontalStretch.Grow;
  }

  protected OnTeamSelected(team: ITeam): void {
    this.WaitingOnSubmit = true;

    this.backend.Team.DeleteTeam(team.id).subscribe({
      complete: () => {
        this.messageSnackBar.OpenConfirmationMessage(
          'Team - ' + team.name + ' deleted successfully'
        );
        this.TeamsUpdated.emit();
        this.WaitingOnSubmit = false;
        this.TeamCollectionUpdated();
        this.notificationChange.OnChange.next();
      },
      error: (errorResp: HttpErrorResponse) => {
        this.messageSnackBar.OpenErrorMessage(errorResp.error);
        this.WaitingOnSubmit = false;
      },
    });
  }
}
