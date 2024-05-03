import { Component, Input } from '@angular/core';
import { IAllAndTeamUsers } from '../../models/IAllAndTeamUsers';
import { IUser } from '../../models/IUser';
import { SelectableElementWrapper } from './selectable-element-wrapper';
import { ITeam } from '../../models/ITeam';
import { BackendConnectService } from '../../services/backend-connect.service';

@Component({
  selector: 'app-add-remove-team-user',
  templateUrl: './add-remove-team-user.component.html',
  styleUrl: './add-remove-team-user.component.scss',
})
export class AddRemoveTeamUserComponent {
  @Input() set DbTeam(dbTeam: ITeam | undefined) {
    if (!dbTeam) return;
    this._dbTeam = dbTeam;
    this.backend.Staff.GetAllAndTeamUsers(dbTeam.id).subscribe({
      next: (val) => {
        this._dbTeamUser = val;
        this.Reset();
      },
    });
  }
  get DbTeam(): ITeam | undefined {
    return this._dbTeam;
  }

  private _dbTeam: ITeam | undefined;
  private _dbTeamUser: IAllAndTeamUsers | undefined;

  public DbSelectedUsers: SelectableElementWrapper<IUser>[] = [];
  public DbNotSelectedUsers: SelectableElementWrapper<IUser>[] = [];

  public UserToAddChange: SelectableElementWrapper<IUser>[] = [];
  public UserToRemoveChange: SelectableElementWrapper<IUser>[] = [];

  constructor(private backend: BackendConnectService) {}

  public UnselectUsers(): void {
    const toRemove = this.DbSelectedUsers.filter((e) => e.IsSelected);
    this.DbSelectedUsers = this.DbSelectedUsers.filter((e) => !e.IsSelected);

    var backToDbState = this.UserToAddChange.filter((e) => e.IsSelected);
    this.UserToAddChange = this.UserToAddChange.filter((e) => !e.IsSelected);

    this.UserToRemoveChange = this.UserToRemoveChange.concat(toRemove);
    this.DbNotSelectedUsers = this.DbNotSelectedUsers.concat(backToDbState);

    this.ClearSelection();
  }

  public SelectUsers(): void {
    const toAdd = this.DbNotSelectedUsers.filter((e) => e.IsSelected);
    this.DbNotSelectedUsers = this.DbNotSelectedUsers.filter(
      (e) => !e.IsSelected
    );

    var backToDbState = this.UserToRemoveChange.filter((e) => e.IsSelected);
    this.UserToRemoveChange = this.UserToRemoveChange.filter(
      (e) => !e.IsSelected
    );

    this.UserToAddChange = this.UserToAddChange.concat(toAdd);
    this.DbSelectedUsers = this.DbSelectedUsers.concat(backToDbState);

    this.ClearSelection();
  }

  private ClearSelection(): void {
    this.DbSelectedUsers.forEach((e) => (e.IsSelected = false));
    this.DbNotSelectedUsers.forEach((e) => (e.IsSelected = false));
    this.UserToAddChange.forEach((e) => (e.IsSelected = false));
    this.UserToRemoveChange.forEach((e) => (e.IsSelected = false));
  }

  private Reset(): void {
    if (this._dbTeamUser) {
      this.DbSelectedUsers = this._dbTeamUser.teamUsers.map(
        (e) => new SelectableElementWrapper(false, e)
      );
      this.DbNotSelectedUsers = this._dbTeamUser.allOtherUsers.map(
        (e) => new SelectableElementWrapper(false, e)
      );
    } else {
      this.DbSelectedUsers = [];
      this.DbNotSelectedUsers = [];
    }

    this.UserToAddChange = [];
    this.UserToRemoveChange = [];
  }

  public Submit(): void {
    if (
      (this.UserToAddChange.length === 0 &&
        this.UserToRemoveChange.length === 0) ||
      !this._dbTeam
    ) {
      return;
    }

    this.backend.Staff.PostTeamChange({
      currentTeam: this._dbTeam,
      toAdd: this.UserToAddChange.map((e) => e.Value),
      toRemove: this.UserToRemoveChange.map((e) => e.Value),
    }).subscribe({
      complete: () => {
        if (this._dbTeam) {
          this.backend.Staff.GetAllAndTeamUsers(this._dbTeam.id).subscribe({
            next: (val) => {
              this._dbTeamUser = val;
              this.Reset();
            },
          });
        }
      },
    });
  }
}
