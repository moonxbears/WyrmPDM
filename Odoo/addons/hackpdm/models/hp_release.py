from odoo import models, fields, api

class hp_release(models.Model):
    _name = 'hp.release'
    _description = 'hp release'
    _inherit = 'hp.common.model'
    _inherit = 'hp.common.model'

    release_note = fields.Char(
        string='release note',
    )

    release_stamp = fields.Datetime(
        string='time stamp',
        default=lambda self:fields.Datetime.now(),
    )

    release_version_id = fields.Many2one(
        comodel_name='hp.version',
    )
    release_user_id = fields.Many2one(
        comodel_name='res.users',
        string='release user',
    )
    entry_id = fields.Many2one(
        comodel_name='hp.entry',
        string='entry'
    )


class hp_release_version_rel(models.Model):
    _name = 'hp.release.version.rel'
    _description = 'hp release relative version'
    _inherit = 'hp.common.model'

    release_id = fields.Many2one(
        comodel_name='hp.release',
        string='release id',
    )
    release_version = fields.Many2one(
        comodel_name='hp.version',
        string='release version',
    )
    release_user = fields.Many2one(
        comodel_name='res.users',
        related='release_id.release_user_id',
        string='relative user release',
    )